﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PageRank
{
    public class IterativePageRank
    {
        protected int mPageCount;
        protected bool[,] mL;
        protected float[] mC;
        protected float mAlpha;
        protected bool mParallel = true;

        public bool Parallel
        {
            get { return mParallel; }
            set { mParallel = value; }
        }

        public IterativePageRank(int page_count, float damping_factor = 0.85f)
        {
            mPageCount = page_count;
            mAlpha = damping_factor;

            mL = new bool[page_count, page_count];
            mC = new float[page_count];
        }

        public void AddLink(int from_page_index, int to_page_index)
        {
            mL[from_page_index, to_page_index] = true;
        }

        private void SetPageOutLinkCount(int page_index, int out_link_count)
        {
            if (out_link_count > 0)
            {
                mC[page_index] = 1.0f / out_link_count;
            }
        }

        private int[] CollectOutLinkCount()
        {
            int[] result = new int[mPageCount];
            for (int i = 0; i < mPageCount; ++i)
            {
                int count = 0;
                for (int j = 0; j < mPageCount; ++j)
                {
                    if (mL[i, j])
                    {
                        count++;
                    }
                }
                result[i] = count;
            }
            return result;
        }

        public float[] RankPages(double tolerance)
        {
            int[] outLinkCount = CollectOutLinkCount();
            for (int i = 0; i < mPageCount; ++i)
            {
                SetPageOutLinkCount(i, outLinkCount[i]);
            }

            float[] P = new float[mPageCount];
            float[] P_prev = new float[mPageCount];

            double diff = 0;
			
			for(int i=0; i < mPageCount; ++i)
			{
				P[i] = 1.0f / mPageCount;
			}

            Task[] tasks = null;
            if (mParallel)
            {
                tasks = new Task[mPageCount];
            }

            int iteration = 0;

            float P_val = 0;
            do
            {
                for (int i = 0; i < mPageCount; ++i)
                {
                    P_prev[i] = P[i];
                }

                for (int j = 0; j < mPageCount; ++j)
                {
                    if (mParallel)
                    {
                        var task_j = j;
                        tasks[j] = Task.Factory.StartNew(() =>
                        {
                            float val = 0;
                            for (int i = 0; i < mPageCount; ++i)
                            {
                                val += (1 - mAlpha) * (mL[i, task_j] ? mC[i] : 0) * P[i] + mAlpha * P[i] / mPageCount;
                            }
							
                            P[task_j] = val;
                        });
                    }
                    else
                    {
                        P_val = 0;
                        for (int i = 0; i < mPageCount; ++i)
                        {
                            P_val += (1 - mAlpha) * (mL[i, j] ? mC[i] : 0) * P[i] + mAlpha * P[i] / mPageCount;
                        }
                        P[j] = P_val;
                    }
                }

                if (mParallel)
                {
                    Task.WaitAll(tasks);
                }
                iteration++;
                diff = Diff(P, P_prev);
                Console.WriteLine("Iteration: {0}, RMSE: {1}", iteration, diff);
            } while (diff > tolerance);

            return P;
        }

        private double Diff(float[] p1, float[] p2)
        {
            float p_diff = 0;
            float sum = 0;
            for (int i = 0; i < mPageCount; ++i)
            {
                p_diff = p1[i] - p2[i];
                sum += p_diff * p_diff;
             }
            return System.Math.Sqrt(sum);
        }
    }
}
