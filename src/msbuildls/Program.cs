using System;
using Newtonsoft.Json;
using msbuildls.LanguageServer;
using StreamJsonRpc;

var stdIn = Console.OpenStandardInput();
var stdOut = Console.OpenStandardOutput();

var rpc = new JsonRpc(stdOut, stdIn);
var serializer = new JsonSerializer();
var lspLogger = new LspLogger();
var server = new MSBuildLanguageServer(rpc, serializer, lspLogger);

rpc.AddLocalRpcTarget(server);
rpc.StartListening();
await rpc.Completion;