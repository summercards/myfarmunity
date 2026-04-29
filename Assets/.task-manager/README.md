# TaskManager Usage Guide

## Project

- Project Name: 我的农场
- Current Version: v1.3
- Status: in_progress

## How To Use

1. Open this project in the TaskManager desktop app.
2. If the project has no current version, create one first.
3. Before creating a manual task, update the goal document and implementation document.
4. During development, prefer using the "development integration" entry to create or update tasks.
5. Reuse the same integration key for the same API, module, or feature so TaskManager updates the same task.
6. You can also add version tasks directly in the local `Tasks.md` file and refresh/sync them back into TaskManager.

## What Will Be Synced Here

- `.task-manager/README.md`
- `.task-manager/<version>/TargetDocument.md`
- `.task-manager/<version>/ImplementationDocument.md`
- `.task-manager/<version>/Tasks.md`
- `.task-manager.json`

## Current Version Focus

中控模式框架搭建：通过中控系统串联每个系统，让单独系统可以独立迭代且不影响其他系统。
