# README Todo - Documentation Improvement Tracker

This file tracks proposed improvements and revisions for README.md files throughout the ReRightGame project. Agentic models should use this file to document their suggested changes before implementing them.

## Format for New Entries

```markdown
## [TIMESTAMP] - [MODEL_NAME] - [SUMMARY_OF_CHANGES]

**README Files to Modify**: 
- Relative/path/to/README.md
- Another/path/to/README.md

**Modification Type**: [Enhancement/Correction/Addition/Restructure]

**Reason**: 
Brief explanation of why these changes are needed.

**Proposed Changes**:
1. Detailed description of change 1
2. Detailed description of change 2
3. Additional changes...

**Impact Assessment**:
How these changes will improve documentation quality and user understanding.

**Priority**: [High/Medium/Low]

-----------------
```

## Pending Documentation Improvements

<!-- Agentic models: Add your improvement proposals below this line -->

## Completed Documentation Improvements

<!-- Move completed items here with completion timestamp -->

## 2026-03-15 - GitHub Copilot (Claude Sonnet 4.6) - llama.cpp migration + per-NPC context

**README Files Modified**:
- `README.md` (root)
- `LLM.md`
- `Assets/Scripts/README.md`
- `Assets/Scripts/NPC/README.md`
- `Assets/Scripts/ServerFiles/README.md`
- `Assets/Scripts/UnityAIScripts/README.md`
- `Assets/Scripts/UnityAIScripts/UnityAIScripts_Logging.md`

**Modification Type**: Enhancement / Correction

**Reason**: Migration from Python TCP server to llama.cpp in-process inference (LLamaSharp). Per-NPC independent context system implemented and wired. `constData.USING_TCP` renamed to `constData._tcp`. All documentation updated to reflect active architecture.

**Changes**:
1. `README.md`: Updated title, features list, and current progress to reflect llama.cpp as primary path
2. `LLM.md`: Full rewrite — model setup, llama.cpp context flow (step-by-step), legacy TCP path instructions, `_tcp` flag explanation
3. `Assets/Scripts/README.md`: Added `constData.cs` entry documenting `_tcp` flag; updated `Hasher.cs` to note TCP-only scope; added `/UnityAIScripts` directory entry; updated `/ServerFiles` and `DomainReloadHelper` notes
4. `Assets/Scripts/NPC/README.md`: Updated architecture overview, `NPCController` AI integration section, `LLM_NPCController` dispatch description, and AI NPC lifecycle steps for both paths
5. `Assets/Scripts/ServerFiles/README.md`: Added legacy-path notice at top
6. `Assets/Scripts/UnityAIScripts/README.md`: Updated `UnityLLM.cs` section to document `public static model/parameters`, factory, `talk2LLMWithContext`, legacy gate; updated context creation workflow
7. `Assets/Scripts/UnityAIScripts/UnityAIScripts_Logging.md`: Added full implementation log entry for all changed components

-----------------

## Guidelines for Contributors

### Before Adding New Entries:
1. **Read ALL existing README files** to understand current documentation state
2. **Log your analysis** in the appropriate logging files
3. **Verify technical accuracy** of proposed changes
4. **Consider integration impacts** across multiple systems

### Quality Standards:
- **Accuracy**: All technical details must be correct
- **Clarity**: Write for diverse technical backgrounds
- **Completeness**: Cover all relevant aspects
- **Security**: Never expose sensitive information

### Priority Guidelines:
- **High**: Critical technical inaccuracies, missing integration points
- **Medium**: Clarity improvements, additional examples, better organization
- **Low**: Minor formatting, terminology consistency, style improvements

-----------------
