#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Infrastructure.Communication;
using NtoLib.Recipes.MbeTable.Infrastructure.Persistence;

namespace NtoLib.Recipes.MbeTable.Composition.StateMachine.App
{
    /// <summary>
    /// Centralized executor for AppEffects. Runs async operations and dispatches Completed commands.
    /// </summary>
    public class RecipeEffectsHandler
    {
        private readonly AppStateMachine _stateMachine;
        private readonly IRecipePlcSender _recipePlcSender;
        private readonly RecipeViewModel _recipeViewModel;
        private readonly IRecipeFileReader _recipeFileReader;
        private readonly IRecipeFileWriter _recipeFileWriter;

        public RecipeEffectsHandler(
            AppStateMachine stateMachine,
            RecipeViewModel recipeViewModel,
            IRecipePlcSender recipePlcSender,
            IRecipeFileReader recipeFileReader,
            IRecipeFileWriter recipeFileWriter)
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
                case ReadRecipeEffect(var opId, var path):
                    Task.Run(async () => await DoReadRecipeAsync(opId, path));
                    break;

                case SaveRecipeEffect(var opIdS, var pathS):
                    Task.Run(async () => await DoSaveRecipeAsync(opIdS, pathS));
                    break;

                case SendRecipeEffect(var opIdT):
                    Task.Run(async () => await DoSendRecipeAsync(opIdT));
                    break;

                case ReceiveRecipeEffect(var opIdR):
                    Task.Run(async () => await DoReceiveRecipeAsync(opIdR));
                    break;
            }
        }

        private async Task DoReadRecipeAsync(Guid opId, string filePath)
        {
            var readResult = _recipeFileReader.ReadRecipe(filePath);

            if (readResult.IsSuccess)
            {
                _recipeViewModel.SetRecipe(readResult.Value);
                _stateMachine.Dispatch(new LoadRecipeCompleted(opId, true, $"Файл загружен: {filePath}"));
            }
            else
            {
                var list = readResult.Errors.Select(e => e.Message).ToList();
                var errorMsg = string.Join("; ", list);
                _stateMachine.Dispatch(new LoadRecipeCompleted(opId, false, $"Ошибка загрузки: {errorMsg}", list));
            }
        }

        private async Task DoSaveRecipeAsync(Guid opId, string filePath)
        {
            var recipe = _recipeViewModel.GetCurrentRecipe();
            var writeResult = _recipeFileWriter.WriteRecipe(recipe, filePath);

            if (writeResult.IsSuccess)
            {
                _stateMachine.Dispatch(new SaveRecipeCompleted(opId, true, $"Файл сохранён: {filePath}"));
            }
            else
            {
                var list = writeResult.Errors.Select(e => e.Message).ToList();
                var errorMsg = string.Join("; ", list);
                _stateMachine.Dispatch(new SaveRecipeCompleted(opId, false, $"Ошибка сохранения: {errorMsg}", list));
            }
        }

        private async Task DoSendRecipeAsync(Guid opId)
        {
            try
            {
                var recipe = _recipeViewModel.GetCurrentRecipe();
                var sendResult = await _recipePlcSender.SendAndVerifyRecipeAsync(recipe);

                if (sendResult.IsSuccess)
                {
                    _stateMachine.Dispatch(new SendRecipeCompleted(opId, true, "Рецепт передан в контроллер."));
                }
                else
                {
                    var list = sendResult.Errors.Select(e => e.Message).ToList();
                    var errorMsg = string.Join("; ", list);
                    _stateMachine.Dispatch(new SendRecipeCompleted(opId, false, $"Ошибка отправки: {errorMsg}", list));
                }
            }
            catch (Exception ex)
            {
                _stateMachine.Dispatch(new SendRecipeCompleted(opId, false, $"Критическая ошибка: {ex.Message}", new[] { ex.Message }));
            }
        }

        private async Task DoReceiveRecipeAsync(Guid opId)
        {
            try
            {
                var result = await _recipePlcSender.ReciveRecipeAsync();
                if (result.IsSuccess)
                {
                    _recipeViewModel.SetRecipe(result.Value);
                    _stateMachine.Dispatch(new ReadRecipeCompleted(opId, true, "Рецепт прочитан из контроллера."));
                }
                else
                {
                    var list = result.Errors.Select(e => e.Message).ToList();
                    var errorMsg = string.Join("; ", list);
                    _stateMachine.Dispatch(new ReadRecipeCompleted(opId, false, $"Ошибка чтения: {errorMsg}", list));
                }
            }
            catch (Exception ex)
            {
                _stateMachine.Dispatch(new ReadRecipeCompleted(opId, false, $"Критическая ошибка: {ex.Message}", new[] { ex.Message }));
            }
        }
    }
}