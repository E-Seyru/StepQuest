# PowerShell script to replace all Debug.Log calls with Logger calls
# Usage: Run from StepQuest root directory

$rootPath = "Assets\Scripts"
$excludeDirs = @("LeanTween", "TutorialInfo", "Utils\Logger.cs", "Utils\LoggerInitializer.cs", "Utils\LoggerSettings.cs", "Debug\LoggerDebugPanel.cs", "Editor\LoggerControlWindow.cs")

# Category mappings based on file path/context
$categoryMappings = @{
    "Gameplay\Player\ActivityManager" = "ActivityLog"
    "Gameplay\Activities" = "ActivityLog"
    "UI\Components\HarvestingActivityCard" = "ActivityLog"
    "UI\Components\PrimaryActivityCard" = "ActivityLog"
    "UI\Components\CraftingActivityCard" = "ActivityLog"
    "UI\Panels\ActivitiesSectionPanel" = "ActivityLog"
    "UI\Panels\ActivityVariantsPanel" = "ActivityLog"
    "UI\Panels\VariantContainer" = "ActivityLog"
    "UI\Panels\VariantIconContainer" = "ActivityLog"
    "UI\Panels\IconContainer" = "ActivityLog"

    "Gameplay\Combat" = "CombatLog"
    "UI\Panels\CombatSectionPanel" = "CombatLog"
    "UI\Components\EnemyCard" = "CombatLog"

    "Gameplay\Player\InventoryManager" = "InventoryLog"
    "UI\Panels\InventoryPanelUI" = "InventoryLog"
    "UI\Panels\EquipmentPanelUI" = "InventoryLog"
    "UI\Components\ItemActionPanel" = "InventoryLog"
    "UI\Components\DraggedItemVisual" = "InventoryLog"

    "Gameplay\World" = "MapLog"
    "Services\MapManager" = "MapLog"
    "UI\Panels\POI" = "MapLog"
    "Utils\MapToggleButton" = "MapLog"

    "Services\StepTracking" = "StepLog"

    "Gameplay\Dialogue" = "DialogueLog"
    "Gameplay\NPC" = "DialogueLog"
    "UI\Panels\DialoguePanelUI" = "DialogueLog"
    "UI\Panels\NPCInteractionPanel" = "DialogueLog"
    "UI\Panels\SocialSectionPanel" = "DialogueLog"
    "UI\Components\SocialAvatarCard" = "DialogueLog"

    "Gameplay\Progression" = "XpLog"

    "Data\" = "DataLog"
    "Data\Registry" = "DataLog"

    "Editor\" = "EditorLog"
    "Debug\" = "EditorLog"
}

function Get-LogCategory {
    param([string]$filePath)

    foreach ($pattern in $categoryMappings.Keys) {
        if ($filePath -like "*$pattern*") {
            return $categoryMappings[$pattern]
        }
    }

    # Check if it's a UI file
    if ($filePath -like "*\UI\*") {
        return "UILog"
    }

    # Default
    return "General"
}

function Replace-DebugLogs {
    param([string]$file)

    # Skip excluded files
    foreach ($exclude in $excludeDirs) {
        if ($file -like "*$exclude*") {
            Write-Host "Skipping excluded file: $file" -ForegroundColor Yellow
            return
        }
    }

    $category = Get-LogCategory -filePath $file
    $content = Get-Content $file -Raw
    $originalContent = $content

    # Replace Debug.Log with Logger.LogInfo
    $content = $content -replace 'Debug\.Log\(', "Logger.LogInfo("

    # Replace Debug.LogWarning with Logger.LogWarning
    $content = $content -replace 'Debug\.LogWarning\(', "Logger.LogWarning("

    # Replace Debug.LogError with Logger.LogError
    $content = $content -replace 'Debug\.LogError\(', "Logger.LogError("

    # Now add the category parameter before the closing parenthesis
    # Pattern: Logger.LogXXX("message") or Logger.LogXXX($"message") or Logger.LogXXX(variable)
    # We need to find Logger calls that don't already have Logger.LogCategory as second parameter

    # Match Logger.LogInfo/LogWarning/LogError calls and add category if not present
    $content = $content -replace '(Logger\.Log(?:Info|Warning|Error)\([^)]+)(\))', "`$1, Logger.LogCategory.$category`$2"

    # Fix double category additions (in case script is run multiple times)
    $content = $content -replace ', Logger\.LogCategory\.\w+, Logger\.LogCategory\.(\w+)', ', Logger.LogCategory.$1'

    if ($content -ne $originalContent) {
        Set-Content -Path $file -Value $content -NoNewline
        Write-Host "Updated: $file (Category: $category)" -ForegroundColor Green
        return $true
    }

    return $false
}

# Main execution
Write-Host "=== Starting Debug.Log Replacement ===" -ForegroundColor Cyan
Write-Host "Root path: $rootPath" -ForegroundColor Cyan
Write-Host ""

$filesProcessed = 0
$filesUpdated = 0

Get-ChildItem -Path $rootPath -Filter "*.cs" -Recurse | ForEach-Object {
    $filesProcessed++
    if (Replace-DebugLogs -file $_.FullName) {
        $filesUpdated++
    }
}

Write-Host ""
Write-Host "=== Replacement Complete ===" -ForegroundColor Cyan
Write-Host "Files processed: $filesProcessed" -ForegroundColor Cyan
Write-Host "Files updated: $filesUpdated" -ForegroundColor Green
Write-Host ""
Write-Host "NOTE: Please review the changes before committing!" -ForegroundColor Yellow
