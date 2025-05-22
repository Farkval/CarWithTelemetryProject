using Assets.Scripts.MapEditor.Actions;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MapEditor.Controllers
{
    public class UndoRedoController
    {
        readonly int _capacity;
        readonly List<IUndoableAction> _history = new();
        int _cursor = -1;               // индекс последней выполненной операции

        public UndoRedoController(int capacity = 100)
        {
            _capacity = Mathf.Clamp(capacity, 1, 1000);
        }

        public void AddAction(IUndoableAction action)
        {
            // отбрасываем «красную часть» истории, если сделали новую операцию
            if (_cursor < _history.Count - 1)
                _history.RemoveRange(_cursor + 1, _history.Count - _cursor - 1);

            // ограничиваем вместимость
            if (_history.Count == _capacity)
                _history.RemoveAt(0);
            else
                _cursor++;

            _history.Add(action);
        }

        public object Undo()
        {
            if (_cursor < 0) return null;
            var act = _history[_cursor--];
            return act.Undo();
        }

        public object Redo()
        {
            if (_cursor + 1 >= _history.Count) return null;
            var act = _history[++_cursor];
            return act.Redo();
        }

        /* ───────── служебное ───────── */
        public void Clear()
        {
            _history.Clear();
            _cursor = -1;
        }
    }
}
