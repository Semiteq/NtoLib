using FluentResults;

using NtoLib.ConfigLoader.Entities;

namespace NtoLib.ConfigLoader.Facade;

public interface IConfigLoaderService
{
	LoaderDto CurrentConfiguration { get; }
	bool IsLoaded { get; }
	string LastError { get; }
	Result<LoaderDto> Load(string filePath);
	Result Save(string filePath, LoaderDto dto);
	Result SaveAndReload(string filePath, LoaderDto dto);
	LoaderDto CreateEmptyConfiguration();
}
