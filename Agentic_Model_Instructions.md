# Instructions for Agentic Models - ReRightGame Analysis

This document provides instructions for agentic AI models to analyze, understand, and document the ReRightGame project architecture and implementation.

## Project Overview
ReRightGame is a 2D Unity game featuring AI-powered NPCs with LLM integration. The game uses a modular architecture with clear separation of concerns between different game systems.

## Analysis Instructions

### 1. README Analysis Phase
Before making any modifications or suggestions, **MANDATORY**: Read through ALL README.md files in the following order to understand the complete system architecture:

#### Core System Documentation (Read First):
- **Root Scripts**: `Assets/Scripts/README.md`
  - Contains overall architecture overview and system relationships
  - Essential for understanding the complete project structure

#### Subsystem Documentation (Read in Order):
- **Dialog System**: `Assets/Scripts/Dialogs/README.md`
  - Conversation management and AI dialog integration
  
- **NPC System**: `Assets/Scripts/NPC/README.md`
  - AI-powered NPC behavior and LLM integration
  
- **Player System**: `Assets/Scripts/Player/README.md`
  - Player input handling and character control
  
- **Shared Character Systems**: `Assets/Scripts/PlayerNPCcommon/README.md`
  - Common components used by both player and NPCs
  
- **Physics System**: `Assets/Scripts/Physics/README.md`
  - Collision detection and interaction mechanics
  
- **Network Communication**: `Assets/Scripts/ServerFiles/README.md`
  - Unity-Python TCP communication for LLM integration
  
- **Extended API**: `Assets/Scripts/ServerFiles-API/README.md`
  - RESTful API communication for additional services

### 2. Logging Requirements
For EVERY analysis session, you MUST log your observations in the corresponding logging files:

#### Logging File Locations:
- **Core Scripts**: `Assets/Scripts/Scripts_Logging.md`
- **Dialog System**: `Assets/Scripts/Dialogs/Dialogs_Logging.md`
- **NPC System**: `Assets/Scripts/NPC/NPC_Logging.md`
- **Player System**: `Assets/Scripts/Player/Player_Logging.md`
- **Shared Systems**: `Assets/Scripts/PlayerNPCcommon/PlayerNPCcommon_Logging.md`
- **Physics**: `Assets/Scripts/Physics/Physics_Logging.md`
- **Network Layer**: `Assets/Scripts/ServerFiles/ServerFiles_Logging.md`
- **API Layer**: `Assets/Scripts/ServerFiles-API/ServerFiles-API_Logging.md`

#### Logging Format (MANDATORY):
```markdown
## [TIMESTAMP] - [YOUR_MODEL_NAME]
**Component**: [Specific script or system name]
**Observation**: [What you observed, analyzed, or discovered]
**Impact**: [How this affects the system or overall architecture]
**Recommendations**: [Your suggestions for improvements]

---
```

### 3. Documentation Improvement Process

#### Step 1: Analysis
1. Read ALL README files listed above
2. Analyze the code structure and implementation
3. Log your findings in appropriate logging files
4. Identify areas for documentation improvement

#### Step 2: Proposal Creation
When you identify documentation improvements, create entries in:
**File**: `ReadmeTodo.md` (at project root level)

#### Step 3: Implementation
Only after logging your analysis should you proceed with any documentation modifications.

## Critical Guidelines

### Security Considerations
- **NEVER** include specific port numbers, IP addresses, or connection strings in documentation
- **NEVER** expose sensitive configuration details
- Focus on architectural patterns rather than implementation specifics

### Documentation Quality Standards
- **Technical Accuracy**: Ensure all technical details are correct
- **Clarity**: Write for both technical and non-technical audiences
- **Completeness**: Cover all major components and their interactions
- **Consistency**: Maintain consistent terminology and formatting

### Analysis Focus Areas
- **Architecture Patterns**: Document design patterns and architectural decisions
- **Integration Points**: How systems communicate and depend on each other
- **Performance Considerations**: Optimization strategies and bottlenecks
- **Error Handling**: Exception handling and recovery mechanisms
- **Scalability**: How systems handle growth and increased load

## Forbidden Actions
- **DO NOT** change file stucture. Maintain the sructure mentioned in the folder's README docs.

## Expected Deliverables
1. **Comprehensive Logging**: Detailed logs in all relevant logging files
2. **Improvement Proposals**: Structured proposals in ReadmeTodo.md
3. **Updated Documentation**: Enhanced README files based on analysis
4. **Architecture Insights**: Deep understanding of system relationships

## Success Criteria
Your analysis is successful when:
- All README files have been thoroughly reviewed
- Comprehensive logs exist for all analyzed systems
- Clear improvement proposals are documented
- Documentation accurately reflects the current implementation
- Architecture relationships are clearly explained

Remember: Your primary goal is to understand and document the sophisticated AI-powered game architecture while maintaining security and accuracy standards.
