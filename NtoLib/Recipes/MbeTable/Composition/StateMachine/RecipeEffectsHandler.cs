#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Services;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine
{
    /// <summary>
    /// Centralized executor for AppEffects. Runs async operations and dispatches Completed commands.
    /// </summary>
    public class RecipeEffectsHandler
    {
        private readonly AppStateMachine _stateMachine;
        private readonly IRecipePlcSender _recipePlcSender;
        private readonly RecipeViewModel _recipeViewModel;
        private readonly RecipeFileReader _recipeFileReader;
        private readonly RecipeFileWriter _recipeFileWriter;

        public RecipeEffectsHandler(
            AppStateMachine stateMachine,
            RecipeViewModel recipeViewModel,
            IRecipePlcSender recipePlcSender,
            RecipeFileReader recipeFileReader,
            RecipeFileWriter recipeFileWriter)
        {
            _stateMachine = stateMachine;
            _recipeViewModel = recipeViewModel;
            _recipePlcSender = recipePlcSender;
            _recipeFileReader = recipeFileReader;
            _recipeFileWriter = recipeFileWriter;
        }

        public void RunEffect(AppEffect effect)
        {
            switch (effect)
            {
                case LoadRecipeEffect(var opId, var path):
                    Task.Run(async () => await DoLoadRecipeAsync(opId, path));
                    break;

                case SaveRecipeEffect(var opIdS, var pathS):
                    Task.Run(async () => await DoSaveRecipeAsync(opIdS, pathS));
                    break;

                case SendRecipeEffect(var opIdT):
                    Task.Run(async () => await DoSendRecipeAsync(opIdT));
                    break;

                case ReadRecipeEffect(var opIdR):
                    Task.Run(async () => await DoReadRecipeAsync(opIdR));
                    break;
            }
        }

        private async Task DoLoadRecipeAsync(Guid opId, string filePath)
        {
            var (recipe, errors) = _recipeFileReader.Read(filePath);
            if (recipe != null && !errors.Any())
            {
                _recipeViewModel.SetRecipe(recipe);
                _stateMachine.Dispatch(new LoadRecipeCompleted(opId, true, $"Файл загружен: {filePath}"));
            }
            else
            {
                var list = errors?.Select(e => e.Message).ToList() ?? new System.Collections.Generic.List<string> { "Неизвестная ошибка" };
                var errorMsg = string.Join("; ", list);
                _stateMachine.Dispatch(new LoadRecipeCompleted(opId, false, $"Ошибка загрузки: {errorMsg}", list));
            }
        }

        private async Task DoSaveRecipeAsync(Guid opId, string filePath)
        {
            var recipe = _recipeViewModel.GetCurrentRecipe();
            var errors = _recipeFileWriter.Write(recipe, filePath);

            if (!errors.Any())
            {
                _stateMachine.Dispatch(new SaveRecipeCompleted(opId, true, $"Файл сохранён: {filePath}"));
            }
            else
            {
                var list = errors.Select(e => e.Message).ToList();
                var errorMsg = string.Join("; ", list);
                _stateMachine.Dispatch(new SaveRecipeCompleted(opId, false, $"Ошибка сохранения: {errorMsg}", list));
            }
        }

        private async Task DoSendRecipeAsync(Guid opId)
        {
            try
            {
                var recipe = _recipeViewModel.GetCurrentRecipe();
                var result = await _recipePlcSender.UploadAndVerifyAsync(recipe);

                if (result.IsSuccess)
                {
                    _stateMachine.Dispatch(new SendRecipeCompleted(opId, true, "Рецепт передан в контроллер."));
                }
                else
                {
                    var first = result.Errors.FirstOrDefault()?.Message ?? "Неизвестная ошибка отправки";
                    _stateMachine.Dispatch(new SendRecipeCompleted(opId, false, $"Ошибка отправки: {first}", new[] { first }));
                }
            }
            catch (Exception ex)
            {
                _stateMachine.Dispatch(new SendRecipeCompleted(opId, false, $"Критическая ошибка: {ex.Message}", new[] { ex.Message }));
            }
        }

        private async Task DoReadRecipeAsync(Guid opId)
        {
            try
            {
                var result = await _recipePlcSender.DownloadAsync();
                if (result.IsSuccess)
                {
                    _recipeViewModel.SetRecipe(result.Value);
                    _stateMachine.Dispatch(new ReadRecipeCompleted(opId, true, "Рецепт прочитан из контроллера."));
                }
                else
                {
                    var first = result.Errors.FirstOrDefault()?.Message ?? "Неизвестная ошибка при чтении рецепта";
                    _stateMachine.Dispatch(new ReadRecipeCompleted(opId, false, $"Ошибка чтения: {first}", new[] { first }));
                }
            }
            catch (Exception ex)
            {
                _stateMachine.Dispatch(new ReadRecipeCompleted(opId, false, $"Критическая ошибка: {ex.Message}", new[] { ex.Message }));
            }
        }
    }
}