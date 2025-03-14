#!/bin/bash
# MVC Migration Script for Kardx
# This script helps to migrate files to the new MVC folder structure

# Function to migrate a file with namespace update
migrate_file() {
    SOURCE_PATH=$1
    TARGET_PATH=$2
    OLD_NAMESPACE=$3
    NEW_NAMESPACE=$4
    
    echo "Migrating $SOURCE_PATH to $TARGET_PATH"
    
    # Create target directory if it doesn't exist
    mkdir -p $(dirname "$TARGET_PATH")
    
    # Copy file with namespace update
    sed "s/namespace $OLD_NAMESPACE/namespace $NEW_NAMESPACE/" "$SOURCE_PATH" > "$TARGET_PATH"
    
    echo "  - Updated namespace from $OLD_NAMESPACE to $NEW_NAMESPACE"
}

# Base directories
BASE_DIR="/Users/hugh/dustland/Kardx"
SCRIPTS_DIR="$BASE_DIR/Assets/Scripts"

# Ensure directory structure exists
mkdir -p "$SCRIPTS_DIR/Models/Cards"
mkdir -p "$SCRIPTS_DIR/Models/Players"
mkdir -p "$SCRIPTS_DIR/Models/Game"
mkdir -p "$SCRIPTS_DIR/Models/Abilities"
mkdir -p "$SCRIPTS_DIR/Models/Abilities/Acting"
mkdir -p "$SCRIPTS_DIR/Models/Abilities/Planning"
mkdir -p "$SCRIPTS_DIR/Views/Cards"
mkdir -p "$SCRIPTS_DIR/Views/Battlefield"
mkdir -p "$SCRIPTS_DIR/Views/Hand"
mkdir -p "$SCRIPTS_DIR/Views/Match"
mkdir -p "$SCRIPTS_DIR/Controllers/DragHandlers"
mkdir -p "$SCRIPTS_DIR/Controllers/InputHandlers"
mkdir -p "$SCRIPTS_DIR/Managers"
mkdir -p "$SCRIPTS_DIR/Utils"

echo "MVC Migration for Kardx"
echo "======================="
echo "Directory structure created."

# Migrate Models
echo -e "\nMigrating Model files..."

# Card models
migrate_file "$SCRIPTS_DIR/Core/Card.cs" "$SCRIPTS_DIR/Models/Cards/Card.cs" "Kardx.Core" "Kardx.Models.Cards"
migrate_file "$SCRIPTS_DIR/Core/CardType.cs" "$SCRIPTS_DIR/Models/Cards/CardType.cs" "Kardx.Core" "Kardx.Models.Cards"
migrate_file "$SCRIPTS_DIR/Core/Deck.cs" "$SCRIPTS_DIR/Models/Cards/Deck.cs" "Kardx.Core" "Kardx.Models.Cards"
migrate_file "$SCRIPTS_DIR/Core/Hand.cs" "$SCRIPTS_DIR/Models/Cards/Hand.cs" "Kardx.Core" "Kardx.Models.Cards"
migrate_file "$SCRIPTS_DIR/Core/CardCollection.cs" "$SCRIPTS_DIR/Models/Cards/CardCollection.cs" "Kardx.Core" "Kardx.Models.Cards"
migrate_file "$SCRIPTS_DIR/Core/Board.cs" "$SCRIPTS_DIR/Models/Cards/Board.cs" "Kardx.Core" "Kardx.Models.Cards"

# Player models
migrate_file "$SCRIPTS_DIR/Core/Player.cs" "$SCRIPTS_DIR/Models/Players/Player.cs" "Kardx.Core" "Kardx.Models.Players"

# Game models
migrate_file "$SCRIPTS_DIR/Core/MatchManager.cs" "$SCRIPTS_DIR/Models/Game/MatchManager.cs" "Kardx.Core" "Kardx.Models.Game"
migrate_file "$SCRIPTS_DIR/Core/Battlefield.cs" "$SCRIPTS_DIR/Models/Game/Battlefield.cs" "Kardx.Core" "Kardx.Models.Game"
migrate_file "$SCRIPTS_DIR/Core/Enums.cs" "$SCRIPTS_DIR/Models/Game/Enums.cs" "Kardx.Core" "Kardx.Models.Game"
migrate_file "$SCRIPTS_DIR/Core/AttackResult.cs" "$SCRIPTS_DIR/Models/Game/AttackResult.cs" "Kardx.Core" "Kardx.Models.Game"
migrate_file "$SCRIPTS_DIR/Core/Condition.cs" "$SCRIPTS_DIR/Models/Game/Condition.cs" "Kardx.Core" "Kardx.Models.Game"
migrate_file "$SCRIPTS_DIR/Core/Modifier.cs" "$SCRIPTS_DIR/Models/Game/Modifier.cs" "Kardx.Core" "Kardx.Models.Game"
migrate_file "$SCRIPTS_DIR/Core/EffectType.cs" "$SCRIPTS_DIR/Models/Game/EffectType.cs" "Kardx.Core" "Kardx.Models.Game"

# Ability models
migrate_file "$SCRIPTS_DIR/Core/Ability.cs" "$SCRIPTS_DIR/Models/Abilities/Ability.cs" "Kardx.Core" "Kardx.Models.Abilities"
migrate_file "$SCRIPTS_DIR/Core/AbilityType.cs" "$SCRIPTS_DIR/Models/Abilities/AbilityType.cs" "Kardx.Core" "Kardx.Models.Abilities"
migrate_file "$SCRIPTS_DIR/Core/Acting/AbilitySystem.cs" "$SCRIPTS_DIR/Models/Abilities/Acting/AbilitySystem.cs" "Kardx.Core.Acting" "Kardx.Models.Abilities.Acting"
migrate_file "$SCRIPTS_DIR/Core/Acting/ISpecialEffectHandler.cs" "$SCRIPTS_DIR/Models/Abilities/Acting/ISpecialEffectHandler.cs" "Kardx.Core.Acting" "Kardx.Models.Abilities.Acting"
migrate_file "$SCRIPTS_DIR/Core/Acting/StrategicDecisionHandler.cs" "$SCRIPTS_DIR/Models/Abilities/Acting/StrategicDecisionHandler.cs" "Kardx.Core.Acting" "Kardx.Models.Abilities.Acting"

# Planning models
migrate_file "$SCRIPTS_DIR/Core/Planning/Decision.cs" "$SCRIPTS_DIR/Models/Abilities/Planning/Decision.cs" "Kardx.Core.Planning" "Kardx.Models.Abilities.Planning"
migrate_file "$SCRIPTS_DIR/Core/Planning/DummyStrategyProvider.cs" "$SCRIPTS_DIR/Models/Abilities/Planning/DummyStrategyProvider.cs" "Kardx.Core.Planning" "Kardx.Models.Abilities.Planning"
migrate_file "$SCRIPTS_DIR/Core/Planning/IStrategyProvider.cs" "$SCRIPTS_DIR/Models/Abilities/Planning/IStrategyProvider.cs" "Kardx.Core.Planning" "Kardx.Models.Abilities.Planning"
migrate_file "$SCRIPTS_DIR/Core/Planning/Strategy.cs" "$SCRIPTS_DIR/Models/Abilities/Planning/Strategy.cs" "Kardx.Core.Planning" "Kardx.Models.Abilities.Planning"
migrate_file "$SCRIPTS_DIR/Core/Planning/StrategyPlanner.cs" "$SCRIPTS_DIR/Models/Abilities/Planning/StrategyPlanner.cs" "Kardx.Core.Planning" "Kardx.Models.Abilities.Planning"

# Migrate Views
echo -e "\nMigrating View files..."

# Card views
migrate_file "$SCRIPTS_DIR/UI/CardView.cs" "$SCRIPTS_DIR/Views/Cards/CardView.cs" "Kardx.UI" "Kardx.Views.Cards"
migrate_file "$SCRIPTS_DIR/UI/CardDetailView.cs" "$SCRIPTS_DIR/Views/Cards/CardDetailView.cs" "Kardx.UI" "Kardx.Views.Cards"

# Hand views
migrate_file "$SCRIPTS_DIR/UI/HandView.cs" "$SCRIPTS_DIR/Views/Hand/HandView.cs" "Kardx.UI" "Kardx.Views.Hand"

# Battlefield views
migrate_file "$SCRIPTS_DIR/UI/BaseBattlefieldView.cs" "$SCRIPTS_DIR/Views/Battlefield/BaseBattlefieldView.cs" "Kardx.UI" "Kardx.Views.Battlefield"
migrate_file "$SCRIPTS_DIR/UI/PlayerBattlefieldView.cs" "$SCRIPTS_DIR/Views/Battlefield/PlayerBattlefieldView.cs" "Kardx.UI" "Kardx.Views.Battlefield"
migrate_file "$SCRIPTS_DIR/UI/OpponentBattlefieldView.cs" "$SCRIPTS_DIR/Views/Battlefield/OpponentBattlefieldView.cs" "Kardx.UI" "Kardx.Views.Battlefield"
migrate_file "$SCRIPTS_DIR/UI/PlayerCardSlot.cs" "$SCRIPTS_DIR/Views/Battlefield/PlayerCardSlot.cs" "Kardx.UI" "Kardx.Views.Battlefield"
migrate_file "$SCRIPTS_DIR/UI/OpponentCardSlot.cs" "$SCRIPTS_DIR/Views/Battlefield/OpponentCardSlot.cs" "Kardx.UI" "Kardx.Views.Battlefield"

# Match views
migrate_file "$SCRIPTS_DIR/UI/MatchView.cs" "$SCRIPTS_DIR/Views/Match/MatchView.cs" "Kardx.UI" "Kardx.Views.Match"
migrate_file "$SCRIPTS_DIR/UI/AttackArrow.cs" "$SCRIPTS_DIR/Views/Match/AttackArrow.cs" "Kardx.UI" "Kardx.Views.Match"

# Migrate Controllers
echo -e "\nMigrating Controller files..."

# Drag handlers
migrate_file "$SCRIPTS_DIR/UI/UnitDeployDragHandler.cs" "$SCRIPTS_DIR/Controllers/DragHandlers/UnitDeployDragHandler.cs" "Kardx.UI" "Kardx.Controllers.DragHandlers"
migrate_file "$SCRIPTS_DIR/UI/OrderDeployDragHandler.cs" "$SCRIPTS_DIR/Controllers/DragHandlers/OrderDeployDragHandler.cs" "Kardx.UI" "Kardx.Controllers.DragHandlers"
migrate_file "$SCRIPTS_DIR/UI/AbilityDragHandler.cs" "$SCRIPTS_DIR/Controllers/DragHandlers/AbilityDragHandler.cs" "Kardx.UI" "Kardx.Controllers.DragHandlers"
migrate_file "$SCRIPTS_DIR/UI/OrderDropHandler.cs" "$SCRIPTS_DIR/Controllers/DragHandlers/OrderDropHandler.cs" "Kardx.UI" "Kardx.Controllers.DragHandlers"

# Migrate Managers
echo -e "\nMigrating Manager files..."
migrate_file "$SCRIPTS_DIR/UI/ViewManager.cs" "$SCRIPTS_DIR/Managers/ViewManager.cs" "Kardx.UI" "Kardx.Managers"
migrate_file "$SCRIPTS_DIR/UI/ViewRegistry.cs" "$SCRIPTS_DIR/Managers/ViewRegistry.cs" "Kardx.UI" "Kardx.Managers"
migrate_file "$SCRIPTS_DIR/UI/TabManager.cs" "$SCRIPTS_DIR/Managers/TabManager.cs" "Kardx.UI" "Kardx.Managers"

# Migrate Utils (keep existing)
echo -e "\nMigrating Utils files..."
# Utils are already in the right place, just update namespaces if needed

echo -e "\nMigration completed."
echo "Note: You will need to fix namespace references in files and update imports."
echo "This script has only moved files and updated their own namespace declarations."
echo "You will still need to update references in those files to point to the new namespaces."
