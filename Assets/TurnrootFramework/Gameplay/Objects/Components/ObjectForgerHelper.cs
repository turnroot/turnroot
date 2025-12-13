using Turnroot.Gameplay.Brain;

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
            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Check if the item can be forged given the current storehouse resources.
        /// This is for UI bindings- it doesn't care WHY it can't be forged
        /// </summary>
        /// <param name="storehouseBrain">
        /// The storehouse brain to check resources against
        /// </param>
        /// <returns></returns>
        public bool CanForge(StorehouseBrain storehouseBrain, ForgeOption selectedForgeOption)
        {
            if (
                forgeOptions == null
                || forgeOptions.Length == 0
                || !storehouseBrain.CanAfford(selectedForgeOption.Price)
                || !storehouseBrain.HasMaterials(
                    selectedForgeOption.Item,
                    selectedForgeOption.ItemAmount
                )
            )
            {
                // failed for any reason, doesn't matter why yet
                return false;
            }
            // good to go!
            return true;
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
            if (!canForgeResult)
            {
                return OperationResult.Failure("Cannot forge item.");
            }

            // spend gold
            var spendGoldResult = storehouseBrain.SpendGold(option.Price);
            if (!spendGoldResult.Success)
            {
                return spendGoldResult;
            }

            // use materials
            var useMaterialsResult = storehouseBrain.ConsumeMaterials(
                option.Item,
                option.ItemAmount
            );
            return !useMaterialsResult.Success
                ? useMaterialsResult
                : OperationResult.SuccessResult();
        }
    }
}
