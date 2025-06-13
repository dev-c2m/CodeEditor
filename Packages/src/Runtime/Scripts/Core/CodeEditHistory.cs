using System;
using System.Collections.Generic;
using UnityEngine;

namespace C2M.CodeEditor
{
    public class CodeEditHistory
    {
        private LinkedList<EditHistoryData> undoList = new LinkedList<EditHistoryData>();
        private LinkedList<EditHistoryData> redoList = new LinkedList<EditHistoryData>();
        private int maxHistory = 0;
        private Action<string, int> applyHistoryAction;
        private float lastUpdateTime = 0f;
        private float undoIdleThresholdSeconds = 0.5f;

        private readonly int DEFAULT_MAX_HISTORY_SIZE = 50;

        public bool IsEmptyRedoList => redoList.Count == 0;
        public bool IsEmptyUndoList => undoList.Count == 0;
        public bool CanUpdateUndo => Time.time - lastUpdateTime >= undoIdleThresholdSeconds;


        public CodeEditHistory(float undoIdleThresholdSeconds, Action<string, int> applyHistoryAction)
        {
            maxHistory = DEFAULT_MAX_HISTORY_SIZE;
            this.applyHistoryAction = applyHistoryAction;
            this.undoIdleThresholdSeconds = undoIdleThresholdSeconds;
        }

        public CodeEditHistory(int maxUndoSize)
        {
            this.maxHistory = maxUndoSize;
        }

        public void AddUndo(string text, int stringPosition)
        {
            if (undoList.Count > maxHistory)
            {
                undoList.RemoveFirst();
            }

            EditHistoryData data = new EditHistoryData(text, stringPosition);
            undoList.AddLast(data);
        }

        public void AddRedo(string text, int stringPosition)
        {
            EditHistoryData data = new EditHistoryData(text, stringPosition);
            redoList.AddLast(data);
        }

        public void ClearRedo()
        {
            redoList.Clear();
        }

        public void Undo()
        {
            if (undoList.Count == 0)
                return;

            EditHistoryData data = undoList.Last.Value;
            undoList.RemoveLast();

            applyHistoryAction?.Invoke(data.Text, data.StringPosition);
        }

        public void Redo()
        {
            if (redoList.Count == 0)
                return;

            EditHistoryData data = redoList.Last.Value;
            redoList.RemoveLast();

            applyHistoryAction?.Invoke(data.Text, data.StringPosition);
        }

        public void UpdateTime()
        {
            lastUpdateTime = Time.time;
        }
    }
}