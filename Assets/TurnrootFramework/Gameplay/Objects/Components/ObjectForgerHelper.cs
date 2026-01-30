using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Components
{
    public class ObjectForgerHelper
    {
        public ObjectItem ThisItem;
        public ForgeOption[] forgeOptions;

        public OperationResult GetForgeOptions()
        {
            if (ThisItem == null || !ThisItem.Forgeable)
            {
                return OperationResult.Failure("Item is not forgeable.");
            }

            forgeOptions = ThisItem.ForgeOptions;
            return OperationResult.Successful();
        }

        /// <summary>
        /// Check if the item can be forged given the current storehouse resources.
        /// This is for UI bindings- it doesn't care WHY it can't be forged
        /// </summary>
        /// <param name="storehouseBrain">
        /// The storehouse brain to check resources against
        /// </param>
        /// <returns></returns>
        public OperationResult CanForge(
            StorehouseBrain storehouseBrain,
            ForgeOption selectedForgeOption
        )
        {
            // Validate basic state
            if (ThisItem == null || !ThisItem.Forgeable)
            {
                return OperationResult.Failure("Item is not forgeable.");
            }

            // Ensure there are options to choose
            if (forgeOptions == null || forgeOptions.Length == 0)
            {
                return OperationResult.Failure("No forge options available.");
            }

            // Validate selected option
            bool optionExists = false;
            foreach (var opt in forgeOptions)
            {
                if (
                    opt.ForgeInto == selectedForgeOption.ForgeInto
                    && opt.Price == selectedForgeOption.Price
                    && opt.Item == selectedForgeOption.Item
                    && opt.ItemAmount == selectedForgeOption.ItemAmount
                )
                {
                    optionExists = true;
                    break;
                }
            }

            if (!optionExists)
            {
                return OperationResult.Failure("Selected forge option is not available.");
            }

            if (storehouseBrain == null)
            {
                return OperationResult.Failure("Storehouse not available.");
            }

            if (!storehouseBrain.CanAfford(selectedForgeOption.Price))
            {
                return OperationResult.Failure(
                    $"Insufficient gold to forge (need {selectedForgeOption.Price})."
                );
            }

            if (
                !storehouseBrain.HasMaterials(
                    selectedForgeOption.Item,
                    selectedForgeOption.ItemAmount
                )
            )
            {
                int available = storehouseBrain.GetMaterialCount(selectedForgeOption.Item);
                return OperationResult.Failure(
                    $"Insufficient materials: need {selectedForgeOption.ItemAmount}, have {available}."
                );
            }

            // good to go!
            return OperationResult.Successful();
        }

        /// <summary>
        /// Forge the item, consuming resources from the storehouse.
        /// </summary>
        /// <param name="storehouseBrain">
        /// The storehouse brain to consume resources from
        /// </param>
        /// <returns>
        /// OperationResult indicating success or failure of the forging process.
        /// </returns>
        public OperationResult ForgeItem(StorehouseBrain storehouseBrain, ForgeOption option)
        {
            var canForgeResult = CanForge(storehouseBrain, option);
            if (!canForgeResult.Success)
            {
                TurnrootLogger.Log(
                    $"ForgeItem failed: {canForgeResult.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
                return canForgeResult;
            }

            // spend gold
            var spendGoldResult = storehouseBrain.SpendGold(option.Price);
            if (!spendGoldResult.Success)
            {
                TurnrootLogger.Log(
                    $"ForgeItem failed: {spendGoldResult.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
                return spendGoldResult;
            }

            // use materials
            var useMaterialsResult = storehouseBrain.ConsumeMaterials(
                option.Item,
                option.ItemAmount
            );

            if (!useMaterialsResult.Success)
            {
                TurnrootLogger.Log(
                    $"ForgeItem failed: {useMaterialsResult.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
                return useMaterialsResult;
            }
            return OperationResult.Successful();
        }
    }
}
