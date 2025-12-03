using FluentResults;

namespace NtoLib.PinConnector.Facade;

public interface IPinConnectorService
{
	Result Enqueue(string sourcePath, string targetPath);
	Result FlushPending();
}
