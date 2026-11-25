using Avalonia.Input;
using DragDropSample.ViewModels;
using MonacoLabs.DragDrop;
using System.Collections.ObjectModel;
using System.Linq;

namespace DragDropSample.CustomDragDrop;

/// <summary>
/// Adds specialized validation to check if the worker is a fit for the team
/// </summary>
public class WorkerDropOperation : CollectionDropOperation
{
    protected override bool CanDrop(DragEventArgs e)
    {
        if (!CanGetPayload(e) || !TryGetDragInfo<CollectionDragInfo>(e, out var metadata))
            return false;

        // Ensure various data are available
        if (metadata.PayloadCollection is not ObservableCollection<WorkerViewModel> workerSource // Not necessary for this validation
            || PayloadTarget is not ObservableCollection<WorkerViewModel> workerDest // Not necessary for this validation
            || !TryGetPayload<WorkerViewModel>(e, out var worker)
            || AttachedControl is not { DataContext: ListBoxPageViewModel vm })
            return false;

        // InteractionIds adds some extra necessary information since we didn't create a specialized TeamViewModel type
        return InteractionIds switch
        {
            ["app:mining"] => Validate(worker, worker.MiningProficiency, vm.MiningTeam, vm.MiningBudgetRemaining),
            ["app:crafting"] => Validate(worker, worker.CraftingProficiency, vm.CraftingTeam, vm.CraftingBudgetRemaining),
            ["app:combat"] => Validate(worker, worker.CombatProficiency, vm.CombatTeam, vm.CombatBudgetRemaining),
            _ => false
        };
    }

    private bool Validate(WorkerViewModel person, int? proficiency, ObservableCollection<WorkerViewModel> team, int budgetRemaining)
    {
        if (proficiency is null)
        {
            return Reject("Worker is not proficient in this area");
        }

        if (budgetRemaining - person.Salary < 0)
        {
            return Reject("Team budget does not allow");
        }

        if (person.DislikedWorkers.Any(team.Contains))
        {
            return Reject($"{person.Name} doesn't like someone in the team");
        }

        if (team.SelectMany(x => x.DislikedWorkers).Contains(person))
        {
            return Reject($"Someone in the team doesn't like {person.Name}");
        }

        return true;
    }
}
