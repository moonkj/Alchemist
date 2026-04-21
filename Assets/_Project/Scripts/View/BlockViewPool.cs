using UnityEngine;

namespace Alchemist.View
{
    /// <summary>
    /// Pre-allocated pool of <see cref="BlockView"/>. Capacity 120 covers a full 6x7 board (42)
    /// plus two fall-through buffers and animation-in-flight overlap. Grow-on-demand logs a
    /// warning (R5: block pool exhaustion) but does not throw.
    /// Stack-based LIFO — most-recently-freed view warm in texture cache first.
    /// Zero GC in steady state (array is pre-sized; only grow path allocates).
    /// </summary>
    public sealed class BlockViewPool
    {
        public const int DefaultCapacity = 120;

        private readonly Transform _parent;
        private readonly BlockView _prefab;
        private BlockView[] _stack;
        private int _top; // next free slot (count of pooled items)
        private int _totalSpawned;

        public int Capacity => _stack.Length;
        public int Available => _top;
        public int TotalSpawned => _totalSpawned;

        public BlockViewPool(BlockView prefab, Transform parent, int capacity = DefaultCapacity)
        {
            if (prefab == null) { Debug.LogError("[BlockViewPool] prefab is null"); }
            _prefab = prefab;
            _parent = parent;
            _stack = new BlockView[capacity];
            Prewarm(capacity);
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _stack[i] = CreateInstance();
            }
            _top = count;
        }

        private BlockView CreateInstance()
        {
            BlockView v = Object.Instantiate(_prefab, _parent);
            v.gameObject.SetActive(false);
            _totalSpawned++;
            return v;
        }

        /// <summary>Rent an inactive BlockView. Grows the backing array on demand with warning.</summary>
        public BlockView Rent()
        {
            if (_top == 0)
            {
                Debug.LogWarning($"[BlockViewPool] Grow-on-demand triggered (capacity={_stack.Length}, spawned={_totalSpawned}).");
                // Create directly — do NOT grow stack (stack is only for pooled items).
                var v = CreateInstance();
                v.gameObject.SetActive(true);
                return v;
            }

            _top--;
            var view = _stack[_top];
            _stack[_top] = null;
            view.gameObject.SetActive(true);
            return view;
        }

        /// <summary>Return a view to the pool. Grows stack array (rare) if full.</summary>
        public void Return(BlockView view)
        {
            if (view == null) return;
            view.Unbind();
            view.gameObject.SetActive(false);

            if (_top >= _stack.Length)
            {
                int newLen = _stack.Length * 2;
                var next = new BlockView[newLen];
                System.Array.Copy(_stack, next, _stack.Length);
                _stack = next;
                Debug.LogWarning($"[BlockViewPool] Stack grew to {newLen}.");
            }
            _stack[_top] = view;
            _top++;
        }
    }
}
