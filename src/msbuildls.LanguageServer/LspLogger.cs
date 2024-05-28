using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace msbuildls.LanguageServer;

internal class LspLogger : ILspLogger
{
    public void LogEndContext(string message, params object[] @params)
    {
        return;
        throw new NotImplementedException();
    }

    public void LogError(string message, params object[] @params)
    {
        return;
        throw new NotImplementedException();
    }

    public void LogException(Exception exception, string? message = null, params object[] @params)
    {
        return;
        throw new NotImplementedException();
    }

    public void LogInformation(string message, params object[] @params)
    {
        return;
        throw new NotImplementedException();
    }

    public void LogStartContext(string message, params object[] @params)
    {
        return;
        throw new NotImplementedException();
    }

    public void LogWarning(string message, params object[] @params)
    {
        return;
        throw new NotImplementedException();
    }
}