using Avalonia.Input;
using DragDropSample.ViewModels;
using MonacoLabs.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Adds specialized validation to check if the dragged workers are a fit for the team
/// </summary>
public class WorkerDropOperation : CollectionDropOperation
{
    protected override bool CanDrop(DragEventArgs e)
    {
        if (!TryGetPayloads<WorkerViewModel>(e, out var workers) || AttachedControl is not { DataContext: ListBoxPageViewModel vm })
            return false;

        // InteractionIds adds some extra necessary information since we didn't create a specialized TeamViewModel type
        return InteractionIds switch
        {
            ["app:mining"] => Validate(workers, x => x.MiningProficiency, vm.MiningTeam, vm.MiningBudgetRemaining),
            ["app:crafting"] => Validate(workers, x => x.CraftingProficiency, vm.CraftingTeam, vm.CraftingBudgetRemaining),
            ["app:combat"] => Validate(workers, x => x.CombatProficiency, vm.CombatTeam, vm.CombatBudgetRemaining),
            _ => false
        };
    }

    private bool Validate(IReadOnlyList<WorkerViewModel> workers, Func<WorkerViewModel, int?> proficiency, ObservableCollection<WorkerViewModel> team,
        int budgetRemaining)
    {
        if (workers.FirstOrDefault(x => proficiency(x) is null) is { } unskilled)
        {
            return Reject($"{unskilled.Name} is not proficient in this area");
        }

        if (budgetRemaining - workers.Sum(x => x.Salary) < 0)
        {
            return Reject(workers.Count == 1 ? "Team budget does not allow" : "Team budget does not allow all of them");
        }

        // The dragged workers join together, so they must also get along with each other
        var newTeam = team.Concat(workers).ToList();
        foreach (var person in workers)
        {
            if (person.DislikedWorkers.Any(newTeam.Contains))
            {
                return Reject($"{person.Name} doesn't like someone in the team");
            }

            if (newTeam.SelectMany(x => x.DislikedWorkers).Contains(person))
            {
                return Reject($"Someone in the team doesn't like {person.Name}");
            }
        }

        return true;
    }
}
