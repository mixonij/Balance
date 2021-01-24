using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Math;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Balance.Helpers
{
    internal class ModelHelpers
    {
        public static ICollection<(int Input, int Output, int FlowNum)> GetExistingFlows(double[,] a)
        {
            var flows = new List<(int, int, int)>();
            for (var k = 0; k < a.Columns(); k++)
            {
                var column = a.GetColumn(k);

                var i = column.IndexOf(-1);
                var j = column.IndexOf(1);

                if (i == -1 || j == -1)
                {
                    continue;
                }

                flows.Add((i, j, k));
            }

            return flows;
        }

        public static double[,] Glr(double[] x0, double[,] a, double[] measurability, double[] tolerance,
            ICollection<(int, int, int)> flows, double globalTest)
        {
            var nodesCount = a.GetLength(0);

            var glrTable = new double[nodesCount, nodesCount];

            if (flows != null)
            {
                foreach (var flow in flows)
                {
                    var (i, j, _) = flow;

                    // Добавляем новый поток
                    var aColumn = new double[nodesCount];
                    aColumn[i] = 1;
                    aColumn[j] = -1;

                    var aNew = a.InsertColumn(aColumn);
                    var x0New = x0.Append(0).ToArray();
                    var measurabilityNew = measurability.Append(0).ToArray();
                    var toleranceNew = tolerance.Append(0).ToArray();

                    // Считаем тест и находим разницу
                    glrTable[i, j] = globalTest - GlobalTest(x0New, aNew, measurabilityNew, toleranceNew);
                }
            }
            else
            {
                for (var i = 0; i < nodesCount; i++)
                {
                    for (var j = i + 1; j < nodesCount; j++)
                    {
                        // Добавляем новый поток
                        var aColumn = new double[nodesCount];
                        aColumn[i] = 1;
                        aColumn[j] = -1;

                        var aNew = a.InsertColumn(aColumn);
                        var x0New = x0.Append(0).ToArray();
                        var measurabilityNew = measurability.Append(0).ToArray();
                        var toleranceNew = tolerance.Append(0).ToArray();

                        // Считаем тест и находим разницу
                        glrTable[i, j] = globalTest - GlobalTest(x0New, aNew, measurabilityNew, toleranceNew);
                    }
                }
            }

            return glrTable;
        }

        public static double GlobalTest(double[] x0, double[,] a, double[] measurability, double[] tolerance)
        {
            var aMatrix = SparseMatrix.OfArray(a);
            var aTransposedMatrix = SparseMatrix.OfMatrix(aMatrix.Transpose());
            var x0Vector = SparseVector.OfEnumerable(x0);

            // Введение погрешностей по неизмеряемым потокам
            var xStd = SparseVector.OfEnumerable(tolerance) / 1.96;

            for (var i = 0; i < xStd.Count; i++)
            {
                if (Math.Abs(measurability[i]) < 0.0000001)
                {
                    xStd[i] = Math.Pow(10, 2) * x0Vector.Maximum();
                }
            }

            var sigma = SparseMatrix.OfDiagonalVector(xStd.PointwisePower(2));

            var r = aMatrix * x0Vector;
            var v = aMatrix * sigma * aTransposedMatrix;

            var result = r * v.PseudoInverse() * r.ToColumnMatrix();
            var chi = ChiSquared.InvCDF(aMatrix.RowCount, 1 - 0.05);

            return result[0] / chi;
        }
    }
}
