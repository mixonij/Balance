using System;
using System.Collections.Generic;
using Balance.Models;
using TreeCollections;

namespace Balance
{
    public interface IBalanceSolver
    {
        double DisbalanceOriginal { get; }
        double Disbalance { get; }
        TimeSpan TimePreparation { get; }
        TimeSpan TimeCalculation { get; }

        double[] Solve(double[] x0, double[,] a, double[] b, double[] measurability, double[] tolerance, double[] lower,
            double[] upper);

        double[] SolveSparse(double[] x0, double[,] a, double[] b, double[] measurability, double[] tolerance,
            double[] lower, double[] upper);

        double GlobalTest(double[] x0, double[,] a, double[] measurability, double[] tolerance);

        (MutableEntityTreeNode<Guid, TreeElement>, List<(int Input, int Output, int FlowNum)>) GetTree(double[] x0, double[,] a, double[] measurability,
            double[] tolerance, int maxSubNodesCount, int maxTreeDepth);
    }
}
