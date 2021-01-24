using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Balance;
using Lab7.Helpers;
using Lab7.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Lab7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BalanceController : ControllerBase
    {
        [HttpPost("text")]
        public async Task<Responce> PostStringAsync([FromForm] string input)
        {
            try
            {
                // Проверка аргумента на null
                _ = input ?? throw new ArgumentNullException(nameof(input));

                // Решение задачи
                var inputData = JsonConvert.DeserializeObject<InputData>(input);
                return await PostAsync(inputData);
            }
            catch (Exception e)
            {
                return new Responce
                {
                    Type = "error",
                    Data = e.Message
                };
            }
        }

        [HttpPost]
        public async Task<Responce> PostAsync([FromBody] InputData input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Проверка аргумента на null
                    _ = input ?? throw new ArgumentNullException(nameof(input));

                    // Решение задачи
                    IBalanceSolver solver = new AccordBalanceSolver();
                    var output = new OutputData
                    {
                        X = solver.Solve(input.X0, input.A, input.B, input.Measurability,
                            input.Tolerance,
                            input.UseTechnologic
                                ? input.LowerTechnologic
                                : input.LowerMetrologic,
                            input.UseTechnologic
                                ? input.UpperTechnologic
                                : input.UpperMetrologic),
                        DisbalanceOriginal = solver.DisbalanceOriginal,
                        Disbalance = solver.Disbalance,
                        TimePreparation = solver.TimePreparation,
                        TimeCalculation = solver.TimeCalculation
                    };

                    return new Responce
                    {
                        Type = "result",
                        Data = output
                    };
                }
                catch (Exception e)
                {
                    return new Responce
                    {
                        Type = "error",
                        Data = e.Message
                    };
                }
            });
        }

        private async Task<Responce> PostSparseAsync([FromBody] InputData input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Проверка аргумента на null
                    _ = input ?? throw new ArgumentNullException(nameof(input));

                    // Решение задачи
                    IBalanceSolver solver = new AccordBalanceSolver();
                    var output = new OutputData
                    {
                        X = solver.SolveSparse(input.X0, input.A, input.B, input.Measurability,
                            input.Tolerance,
                            input.UseTechnologic
                                ? input.LowerTechnologic
                                : input.LowerMetrologic,
                            input.UseTechnologic
                                ? input.UpperTechnologic
                                : input.UpperMetrologic),
                        DisbalanceOriginal = solver.DisbalanceOriginal,
                        Disbalance = solver.Disbalance,
                        TimePreparation = solver.TimePreparation,
                        TimeCalculation = solver.TimeCalculation
                    };

                    return new Responce
                    {
                        Type = "result",
                        Data = output
                    };
                }
                catch (Exception e)
                {
                    return new Responce
                    {
                        Type = "error",
                        Data = e.Message
                    };
                }
            });
        }

        [HttpPost("graph")]
        public async Task<Responce> PostGraphAsync([FromBody] InputGraph input)
        {
            var converted = await GraphHelpers.GraphToMatrixAsync(input);

            return await PostAsync(converted);
        }

        [HttpPost("graph_sparse")]
        public async Task<Responce> PostGraphSparseAsync([FromBody] InputGraph input)
        {
            var converted = await GraphHelpers.GraphToMatrixAsync(input);

            return await PostSparseAsync(converted);
        }

        [HttpPost("global_test")]
        public async Task<Responce> TestAsync([FromBody] InputData input)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Проверка аргумента на null
                    _ = input ?? throw new ArgumentNullException(nameof(input));

                    // Решение задачи
                    IBalanceSolver solver = new AccordBalanceSolver();
                    var output = solver.GlobalTest(input.X0, input.A, input.Measurability,
                        input.Tolerance);

                    return new Responce
                    {
                        Type = "result",
                        Data = output
                    };
                }
                catch (Exception e)
                {
                    return new Responce
                    {
                        Type = "error",
                        Data = e.Message
                    };
                }
            });
        }

        [HttpPost("generate")]
        public async Task<Responce> PostGenerateAsync([FromQuery] int nodesCount, [FromQuery] int flowsCount,
            [FromQuery] double min = -100, [FromQuery] double max = 100)
        {
            return await Task.Run(() =>
            {
                var rand = new Random();

                var nodes = new List<string>();
                for (var i = 0; i < nodesCount; i++)
                {
                    nodes.Add(Guid.NewGuid().ToString());
                }

                var result = new InputGraph
                {
                    BalanceSettings = new BalanceSettings(),
                    Dependencies = null,
                    Variables = new List<Variable>()
                };

                for (var i = 0; i < flowsCount; i++)
                {
                    var destination = rand.Next(-1, nodesCount);
                    var source = rand.Next(-1, nodesCount);

                    var variable = new Variable
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Variable {i}",
                        DestinationId = destination != -1 ? nodes[destination] : null,
                        SourceId = source != -1 ? nodes[source] : null,
                        Measured = min + rand.NextDouble() * (max - min),
                        Tolerance = (min + rand.NextDouble() * (max - min)) / 10.0,
                        MetrologicRange = new Models.Range
                        {
                            Min = min + rand.NextDouble() * (max - min) / 2.0,
                            Max = max - rand.NextDouble() * (max - min) / 2.0,
                        },
                        TechnologicRange = new Models.Range
                        {
                            Min = min + rand.NextDouble() * (max - min) / 2.0,
                            Max = max - rand.NextDouble() * (max - min) / 2.0,
                        },
                        IsMeasured = true,
                        InService = true,
                        VarType = "FLOW"
                    };

                    result.Variables.Add(variable);
                }

                return new Responce
                {
                    Type = "result",
                    Data = result
                };
            });
        }

        [HttpPost("tree")]
        public async Task<Responce> GlrAsync([FromBody] InputGraph inputGraph, int maxSubNodesCount, int maxTreeDepth)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var solver = new AccordBalanceSolver();
                    var input = await GraphHelpers.GraphToMatrixAsync(inputGraph);
                    var (root, flows) = solver.GetTree(input.X0, input.A, input.Measurability,
                        input.Tolerance, maxSubNodesCount, maxTreeDepth);

                    var nodes = root.Where(x => x.IsLeaf);
                    var results = new List<Glr>();

                    foreach (var node in nodes)
                    {
                        var result = new List<Flow>();
                        var flowCorrections = new List<Variable>();

                        foreach (var flow in node.Item.Flows)
                        {
                            var (i, j) = flow;

                            var newFlow = new Flow($"{i} -> {j}");

                            var existingFlowIdx = flows.FindIndex(x => x.Input == i && x.Output == j);
                            if (existingFlowIdx != -1)
                            {
                                var (_, _, existingFlow) = flows[existingFlowIdx];

                                newFlow.Id = input.Guids[existingFlow];
                                newFlow.Name = input.Names[existingFlow];
                                newFlow.Number = existingFlow;

                                var variable = new Variable
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    SourceId = input.NodesGuids[i],
                                    DestinationId = input.NodesGuids[j],
                                    Name = input.Names[existingFlow] + " (Добавочный)",
                                    MetrologicRange = new Models.Range
                                    {
                                        Min = input.LowerMetrologic[existingFlow] - input.X0[existingFlow],
                                        Max = input.UpperMetrologic[existingFlow] - input.X0[existingFlow]
                                    },
                                    TechnologicRange = new Models.Range
                                    {
                                        Min = input.LowerTechnologic[existingFlow] - input.X0[existingFlow],
                                        Max = input.UpperTechnologic[existingFlow] - input.X0[existingFlow]
                                    },
                                    Tolerance = input.Tolerance[existingFlow],
                                    IsMeasured = true,
                                    VarType = "FLOW"
                                };

                                flowCorrections.Add(variable);
                            }

                            result.Add(newFlow);
                        }

                        results.Add(new Glr
                        {
                            FlowErrors = result,
                            FlowCorrections = flowCorrections,
                            TestValue = node.Item.TestValue
                        });
                    }

                    return new Responce
                    {
                        Type = "result",
                        Data = results.OrderBy(x => x.TestValue)
                    };
                }
                catch (Exception e)
                {
                    return new Responce
                    {
                        Type = "error",
                        Data = e.Message
                    };
                }
            });
        }
    }
}
