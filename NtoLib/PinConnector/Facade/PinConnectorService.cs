using System;
using System.Collections.Generic;

using FluentResults;

using MasterSCADA.Hlp;

namespace NtoLib.PinConnector.Facade;

public class PinConnectorService : IPinConnectorService
{
	private readonly IProjectHlp _project;
	private readonly Queue<(string Source, string Target)> _pendingConnections = new();

	public PinConnectorService(IProjectHlp project)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
	}

	public Result Enqueue(string sourcePath, string targetPath)
	{
		if (string.IsNullOrWhiteSpace(sourcePath))
		{
			return Result.Fail("Путь к исходному пину пуст");
		}

		if (string.IsNullOrWhiteSpace(targetPath))
		{
			return Result.Fail("Путь к целевому пину пуст");
		}

		_pendingConnections.Enqueue((sourcePath, targetPath));
		return Result.Ok();
	}

	public Result FlushPending()
	{
		if (_project.InRuntime)
		{
			return Result.Fail("Проект ещё в режиме исполнения");
		}

		var errors = new List<string>();
		while (_pendingConnections.Count > 0)
		{
			var (src, tgt) = _pendingConnections.Dequeue();
			var result = Connect(src, tgt);
			if (result.IsFailed)
			{
				errors.Add($"{src} -> {tgt}: {string.Join(", ", result.Errors)}");
			}
		}

		return errors.Count == 0
			? Result.Ok()
			: Result.Fail(string.Join("; ", errors));
	}

	private Result Connect(string sourcePath, string targetPath)
	{
		var sourcePin = _project.SafeItem<ITreePinHlp>(sourcePath);
		if (sourcePin == null)
		{
			return Result.Fail("Исходный пин не найден в дереве проекта");
		}

		var targetPin = _project.SafeItem<ITreePinHlp>(targetPath);
		if (targetPin == null)
		{
			return Result.Fail("Целевой пин не найден в дереве проекта");
		}

		try
		{
			// Автовыбор типа соединения (Generic/IConnect) на стороне MasterSCADA
			targetPin.Connect(sourcePin);
		}
		catch (Exception ex)
		{
			return Result.Fail($"Ошибка соединения: {ex.Message}");
		}

		return Result.Ok();
	}
}
