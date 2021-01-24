using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Optimization;
using Balance.Helpers;
using Balance.Models;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Storage;
using TreeCollections;
using Matrix = Accord.Math.Matrix;

namespace Balance
{
    public class AccordBalanceSolver : IBalanceSolver
    {
        public double DisbalanceOriginal { get; private set; }
        public double Disbalance { get; private set; }

        public TimeSpan TimePreparation { get; private set; }
        public TimeSpan TimeCalculation { get; private set; }

        public double[] Solve(double[] x0, double[,] a, double[] b, double[] measurability, double[] tolerance, double[] lower, double[] upper)
        {
            // Проверка аргументов на null
            _ = x0 ?? throw new ArgumentNullException(nameof(x0));
            _ = a ?? throw new ArgumentNullException(nameof(a));
            _ = b ?? throw new ArgumentNullException(nameof(b));
            _ = measurability ?? throw new ArgumentNullException(nameof(measurability));
            _ = tolerance ?? throw new ArgumentNullException(nameof(tolerance));
            _ = lower ?? throw new ArgumentNullException(nameof(lower));
            _ = upper ?? throw new ArgumentNullException(nameof(upper));

            //Проверка аргументов на размерности
            if (x0.Length == 0) throw new ArgumentException(nameof(x0));
            if (a.GetLength(1) != x0.Length)
                throw new ArgumentException("Array length by dimension 1 is not equal to X0 length.", nameof(a));
            if (b.Length != a.GetLength(0))
                throw new ArgumentException("Array length is not equal to A length by 0 dimension.", nameof(b));
            if (measurability.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(measurability));
            if (tolerance.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(tolerance));
            if (lower.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(lower));
            if (upper.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(upper));

            var tB = DateTime.Now;

            var i = Matrix.Diagonal(measurability);
            var w = Matrix.Diagonal(1.Divide(tolerance.Pow(2)));

            var h = i.Dot(w);

            var d = h.Dot(x0).Multiply(-1);

            TimePreparation = DateTime.Now - tB;

            var func = new QuadraticObjectiveFunction(h, d);
            var constraints = new List<LinearConstraint>();

            //Нижние и верхние границы
            for (var j = 0; j < x0.Length; j++)
            {
                constraints.Add(new LinearConstraint(1)
                {
                    VariablesAtIndices = new[] { j },
                    ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                    Value = lower[j]
                });

                constraints.Add(new LinearConstraint(1)
                {
                    VariablesAtIndices = new[] { j },
                    ShouldBe = ConstraintType.LesserThanOrEqualTo,
                    Value = upper[j]
                });
            }

            //Ограничения для решения задачи баланса
            for (var j = 0; j < b.Length; j++)
            {
                var notNullElements = Array.FindAll(a.GetRow(j), x => Math.Abs(x) > 0.0000001);
                var notNullElementsIndexes = new List<int>();
                for (var k = 0; k < x0.Length; k++)
                {
                    if (Math.Abs(a[j, k]) > 0.0000001)
                    {
                        notNullElementsIndexes.Add(k);
                    }
                }

                constraints.Add(new LinearConstraint(notNullElements.Length)
                {
                    VariablesAtIndices = notNullElementsIndexes.ToArray(),
                    CombinedAs = notNullElements,
                    ShouldBe = ConstraintType.EqualTo,
                    Value = b[j]
                });
            }

            tB = DateTime.Now;
            var solver = new GoldfarbIdnani(func, constraints);
            if (!solver.Minimize()) throw new ApplicationException("Failed to solve balance task.");

            TimeCalculation = DateTime.Now - tB;


            DisbalanceOriginal = a.Dot(x0).Subtract(b).Euclidean();
            Disbalance = a.Dot(solver.Solution).Subtract(b).Euclidean();


            return solver.Solution;
        }

        public double[] SolveSparse(double[] x0, double[,] a, double[] b, double[] measurability, double[] tolerance, double[] lower, double[] upper)
        {
            // Проверка аргументов на null
            _ = x0 ?? throw new ArgumentNullException(nameof(x0));
            _ = a ?? throw new ArgumentNullException(nameof(a));
            _ = b ?? throw new ArgumentNullException(nameof(b));
            _ = measurability ?? throw new ArgumentNullException(nameof(measurability));
            _ = tolerance ?? throw new ArgumentNullException(nameof(tolerance));
            _ = lower ?? throw new ArgumentNullException(nameof(lower));
            _ = upper ?? throw new ArgumentNullException(nameof(upper));

            //Проверка аргументов на размерности
            if (x0.Length == 0) throw new ArgumentException(nameof(x0));
            if (a.GetLength(1) != x0.Length)
                throw new ArgumentException("Array length by dimension 1 is not equal to X0 length.", nameof(a));
            if (b.Length != a.GetLength(0))
                throw new ArgumentException("Array length is not equal to A length by 0 dimension.", nameof(b));
            if (measurability.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(measurability));
            if (tolerance.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(tolerance));
            if (lower.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(lower));
            if (upper.Length != x0.Length)
                throw new ArgumentException("Array length is not equal to X0 length.", nameof(upper));

            var tB = DateTime.Now;

            var i = Matrix.Diagonal(measurability);
            var w = Matrix.Diagonal((1 / SparseVector.OfVector(new DenseVector(tolerance)).PointwisePower(2)).ToArray());

            var iSparse = SparseMatrix.OfArray(i);
            var wSparse = SparseMatrix.OfArray(w);

            var hSparse = iSparse.Multiply(wSparse);
            var h = hSparse.ToArray();

            var x0Sparse = SparseVector.OfVector(new DenseVector(x0));

            var d = hSparse.Multiply(x0Sparse).Multiply(-1).ToArray();

            TimePreparation = DateTime.Now - tB;

            var func = new QuadraticObjectiveFunction(h, d);
            var constraints = new List<LinearConstraint>();

            //Нижние и верхние границы
            for (var j = 0; j < x0.Length; j++)
            {
                constraints.Add(new LinearConstraint(1)
                {
                    VariablesAtIndices = new[] { j },
                    ShouldBe = ConstraintType.GreaterThanOrEqualTo,
                    Value = lower[j]
                });

                constraints.Add(new LinearConstraint(1)
                {
                    VariablesAtIndices = new[] { j },
                    ShouldBe = ConstraintType.LesserThanOrEqualTo,
                    Value = upper[j]
                });
            }

            //Ограничения для решения задачи баланса
            for (var j = 0; j < b.Length; j++)
            {
                var notNullElements = Array.FindAll(a.GetRow(j), x => Math.Abs(x) > 0.0000001);
                var notNullElementsIndexes = new List<int>();
                for (var k = 0; k < x0.Length; k++)
                {
                    if (Math.Abs(a[j, k]) > 0.0000001)
                    {
                        notNullElementsIndexes.Add(k);
                    }
                }

                constraints.Add(new LinearConstraint(notNullElements.Length)
                {
                    VariablesAtIndices = notNullElementsIndexes.ToArray(),
                    CombinedAs = notNullElements,
                    ShouldBe = ConstraintType.EqualTo,
                    Value = b[j]
                });
            }

            tB = DateTime.Now;

            var solver = new GoldfarbIdnani(func, constraints);
            if (!solver.Minimize()) throw new ApplicationException("Failed to solve balance task.");

            TimeCalculation = DateTime.Now - tB;

            var aSparse = SparseMatrix.OfArray(a);
            var bSparse = SparseVector.OfVector(new DenseVector(b));

            DisbalanceOriginal = aSparse.Multiply(x0Sparse).Subtract(bSparse).ToArray().Euclidean();
            Disbalance = aSparse.Multiply(SparseVector.OfVector(new DenseVector(solver.Solution))).Subtract(bSparse).ToArray().Euclidean();

            return solver.Solution;
        }

        public double GlobalTest(double[] x0, double[,] a, double[] measurability, double[] tolerance)
        {
            return ModelHelpers.GlobalTest(x0, a, measurability, tolerance);
        }

        public (MutableEntityTreeNode<Guid, TreeElement>, List<(int Input, int Output, int FlowNum)>) GetTree(double[] x0, double[,] a, 
            double[] measurability, double[] tolerance, int maxSubNodesCount, int maxTreeDepth)
        {
            var flows = ModelHelpers.GetExistingFlows(a).ToList();
            var nodesCount = a.Rows();

            var rootNode = new MutableEntityTreeNode<Guid, TreeElement>(x => x.Id, new TreeElement());
            var analyzingNode = rootNode;

            while (analyzingNode != null)
            {
                var newMeasurability = measurability;
                var newTolerance = tolerance;
                var newA = a;
                var newX0 = x0;
                
                //Добавляем новые потоки
                foreach (var (newI, newJ) in analyzingNode.Item.Flows)
                {
                    var aColumn = new double[nodesCount];
                    aColumn[newI] = 1;
                    aColumn[newJ] = -1;

                    newMeasurability = newMeasurability.Append(0).ToArray();
                    newTolerance = newTolerance.Append(0).ToArray();

                    newX0 = newX0.Append(0).ToArray();
                    newA = newA.InsertColumn(aColumn);
                }

                //Значение глобального теста
                var gTest = GlobalTest(newX0, newA, newMeasurability,
                    newTolerance);

                //GLR
                var glr = ModelHelpers.Glr(newX0, newA, newMeasurability,
                    newTolerance, flows, gTest);

                var (i, j) = (0, 0);
                for (var k = 0; k < analyzingNode.Children.Count + 1; k++)
                {
                    (i, j) = glr.ArgMax();

                    if (glr[i, j] <= 0)
                    {
                        break;
                    }

                    if (k != analyzingNode.Children.Count)
                    {
                        glr[i, j] = 0.0;
                    }
                }

                //Критерий останова по глубине и узлам
                if (glr[i, j] > 0 && gTest >= 1 && analyzingNode.Children.Count < maxSubNodesCount &&
                    analyzingNode.Level < maxTreeDepth)
                {
                    //Создаем узел
                    var node = new TreeElement(new List<(int, int)>(analyzingNode.Item.Flows), gTest - glr[i, j]);

                    //Добавляем дочерний элемент
                    analyzingNode = analyzingNode.AddChild(node);

                    node.Flows.Add((i, j));

                }
                else
                {
                    //Переназначаем узел
                    analyzingNode = analyzingNode.Parent;
                }
            }

            return (rootNode, flows);
        }
    }
}
