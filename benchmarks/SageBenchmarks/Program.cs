// Sage DES Engine — BenchmarkDotNet baseline
// Run with:  dotnet run -c Release --project Sage_Aux\SageBenchmarks\SageBenchmarks.csproj
// Quick validation run:  dotnet run -c Release -- --filter *
// Specific class:        dotnet run -c Release -- --filter *EventDispatch*

using BenchmarkDotNet.Running;
using System;

// BenchmarkSwitcher discovers all [Benchmark] classes in this assembly and lets
// the caller choose which to run via command-line filters (e.g. --filter *).
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
