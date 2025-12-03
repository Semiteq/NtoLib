using System;
using System.Linq;

using FluentResults;

using MasterSCADA;
using MasterSCADA.Common;
using MasterSCADA.Hlp;
using MasterSCADA.Trend.Controls;
using MasterSCADA.Trend.Helpers;
using MasterSCADA.Trend.Services;

using MasterSCADALib;

namespace NtoLib.TrendPensManager.Facade;

public class TrendPensService : ITrendPensService
{
	private readonly IProjectHlp _project;

	public TrendPensService(IProjectHlp project)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
	}

	public Result Refresh(string trendPath, string pinPath)
	{
		try
		{
			return AddPenToTrendByPath(_project, trendPath, pinPath);
		}
		catch (Exception e)
		{
			return Result.Fail(e.Message);
		}
	}

	private static Result AddPenToTrendByPath(
		IProjectHlp project,
		string trendPath,
		string pinPath,
		string? displayNameOverride = null)
	{
		if (project == null)
		{
			throw new ArgumentNullException(nameof(project));
		}

		if (string.IsNullOrWhiteSpace(trendPath))
		{
			return Result.Fail("Путь к тренду пуст");
		}

		if (string.IsNullOrWhiteSpace(pinPath))
		{
			return Result.Fail("Путь к пину пуст");
		}

		var trendItem = project.SafeItem<ITreeItemHlp>(trendPath);
		if (trendItem == null)
		{
			return Result.Fail("Тренд не найден в дереве проекта");
		}

		var pin = project.SafeItem<ITreePinHlp>(pinPath);
		if (pin == null)
		{
			return Result.Fail("Пин не найден в дереве проекта");
		}

		var trendService = project.GetService<TrendService>();
		if (trendService == null || TrendService.Dispatcher == null)
		{
			return Result.Fail("Сервис трендов недоступен");
		}

		var result = Result.Fail("Не удалось добавить перо в тренд");
		TrendService.Dispatcher.Invoke(() =>
		{
			result = AddPenToTrend(project, trendItem, pin);
		});

		return result;
	}

	private static Result AddPenToTrend(
		IProjectHlp project,
		ITreeItemHlp trendItem,
		ITreePinHlp pin)
	{
		var trend = FindOpenTrendForItem(project, trendItem);
		if (trend == null)
		{
			// the window isn't opened
			return Result.Fail("Окно тренда закрыто");
		}

		SetMaxTrendItems(trend);
		_ = trend.Settings.MaxParameters;

		// check rights
		if (!trend.AddParamsRights ||
			(trend.RuntimeMode && trend.Inited &&
			 !trend.CheckPermissionTrend(Rights.Trends.AddParams, "Добавление пера")))
		{
			return Result.Fail("Нет прав на добавление пера в тренд");
		}

		var graph = trend.AddParametr(pin);
		if (graph == null)
		{
			return Result.Fail("Не удалось добавить перо в тренд");
		}

		var displayNameOverride = pin.Name + "123";

		return SetPenUserName(trend, pin, displayNameOverride);
	}

	private static Trend? FindOpenTrendForItem(IProjectHlp project, ITreeItemHlp trendItem)
	{
		var trendService = project.GetService<TrendService>();
		return trendService != null
			? GetTrendByFullName(trendService, trendItem.FullName)
			: null;
	}

	private static Trend? GetTrendByFullName(TrendService trendService, string trendFullName)
	{
		foreach (var trends in trendService.Opened)
		{
			if (trends.Attribute?.TreeItem != null &&
				string.Equals(trends.Attribute.TreeItem.FullName, trendFullName, StringComparison.OrdinalIgnoreCase))
			{
				return trends;
			}
		}
		return null;

	}

	private static Result SetPenUserName(Trend trend, ITreePinHlp pin, string userName)
	{
		if (trend == null)
		{
			return Result.Fail("Окно тренда не задано");
		}

		if (pin == null)
		{
			return Result.Fail("Пин не задан");
		}

		var pinId = trend.Attribute.GetPinId(pin);
		if (string.IsNullOrWhiteSpace(pinId))
		{
			return Result.Fail("Не удалось определить идентификатор пина");
		}

		// Поиск ScadaPenSettings по PinId среди всех графиков, а не только видимых источников
		var penSettings = trend.Settings.Objects
			.OfType<MasterSCADA.Graph.Objects.BaseGraph2D>()
			.Select(g => g.CustomSettings as ScadaPenSettings)
			.FirstOrDefault(s => s != null && string.Equals(s.PinId, pinId, StringComparison.OrdinalIgnoreCase));

		if (penSettings == null)
		{
			return Result.Fail("Не удалось найти перо в тренде");
		}

		penSettings.UserName = userName;
		penSettings.SavedDT = false; // mark like changed in RT
		trend.UpdateVisibleSources();

		return Result.Ok();
	}

	private static void SetMaxTrendItems(Trend trend, int max = 1000)
	{
		trend.Settings.MaxParameters = max;
	}
}
