using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapEditor
{
    public class UndoRedoManager
    {
        private readonly int _capacity;
        private readonly Stack<IUndoableAction> _undoStack = new();
        private readonly Stack<IUndoableAction> _redoStack = new();

        public UndoRedoManager(int capacity)
        {
            _capacity = Mathf.Clamp(capacity, 1, 1000);
        }

        public void AddAction(IUndoableAction action)
        {
            if (_undoStack.Count >= _capacity)
            {
                // Сбрасываем самый старый
                var temp = new Stack<IUndoableAction>(_undoStack);
                temp.Pop();
                _undoStack.Clear();
                foreach (var a in temp)
                    _undoStack.Push(a);
            }
            _undoStack.Push(action);
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) 
                return;
            var act = _undoStack.Pop();
            act.Undo();
            _redoStack.Push(act);
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) 
                return;
            var act = _redoStack.Pop();
            act.Redo();
            _undoStack.Push(act);
        }
    }
}
