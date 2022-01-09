using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Ccr.Core
{
    internal class CausalityThreadContext
    {
        public CausalityThreadContext(ICausality causality, ICollection<CausalityStack> stacks)
        {
            _activeCausality = causality;
            _stacks = stacks;
        }

        public static bool IsEmpty(CausalityThreadContext context) 
            => context == null || (context._activeCausality == null && context._stacks == null);

        public ICollection<ICausality> Causalities
        {
            get
            {
                if (_activeCausality != null) return new[] { _activeCausality };
                var causalities = new List<ICausality>();
                if (_stacks == null) 
                    return causalities;
                causalities.AddRange(from stack in _stacks where stack.Count != 0 select stack[stack.Count - 1]);
                return causalities;
            }
        }

        private void Normalize()
        {
            if (_activeCausality != null || _stacks == null) return;
            
            var count = 0;
            List<CausalityStack> causaltyStacks = null;
            foreach (var stack in _stacks)
            {
                if (stack.Count == 0)
                {
                    if (causaltyStacks == null) 
                        causaltyStacks = new List<CausalityStack>(1);
                    causaltyStacks.Add(stack);
                }
                count += stack.Count;
            }
            if (causaltyStacks == null && count > 1) return;
            
            if (causaltyStacks != null)
                foreach (var stack in causaltyStacks) 
                    _stacks.Remove(stack);
            
            if (_stacks.Count == 0)  
                _stacks = null;
            
            if (count != 1) return;
            
            _activeCausality = ((List<CausalityStack>)_stacks)?[0][0];
            _stacks = null;
        }
        internal void AddCausality(ICausality causality)
        {
            if (IsEmpty(this))
            {
                _activeCausality = causality;
                return;
            }
            
            if (_causalityTable == null) 
                _causalityTable = new Dictionary<Guid, ICausality>();
            
            if (_activeCausality == null)
            {
                if (_stacks == null) return;
                
                if (_causalityTable.ContainsKey(causality.Guid)) 
                    return;
                
                _causalityTable.Add(causality.Guid, causality);
                foreach (var stack in _stacks) 
                    stack.Add(causality);
                return;
            }
            if (causality.Guid == _activeCausality.Guid) return;
            
            _causalityTable.Add(_activeCausality.Guid, _activeCausality);
            _causalityTable.Add(causality.Guid, causality);
            _stacks = new List<CausalityStack>();
            var newStack = new CausalityStack
            {
                _activeCausality,
                causality
            };
            _activeCausality = null;
            _stacks.Add(newStack);
        }
        internal CausalityThreadContext Clone()
        {
            var ctx = new CausalityThreadContext(_activeCausality, null);
            if (_activeCausality != null) return ctx;
            ctx._stacks = new List<CausalityStack>();
            foreach (var stack in _stacks)
            {
                var newStack = new CausalityStack();
                newStack.AddRange(stack);
                ctx._stacks.Add(newStack);
            }
            return ctx;
        }
        internal bool RemoveCausality(string name, ICausality causality)
        {
            if (_activeCausality != null && (causality != null && causality == _activeCausality || name == _activeCausality.Name))
            {
                RemoveFromTable(name, causality);
                _activeCausality = null;
                return true;
            }
            foreach (var stack in _stacks)
            {
                foreach (var stackCausality in stack.Where(stackCausality =>
                             causality != null && causality == stackCausality || name == stackCausality.Name))
                {
                    stack.Remove(stackCausality);
                    RemoveFromTable(name, causality);
                    Normalize();
                    return true;
                }
            }
            Normalize();
            return false;
        }
        private void RemoveFromTable(string name, ICausality causality)
        {
            if (_causalityTable == null) return;
            
            if (causality != null)
            {
                _causalityTable.Remove(causality.Guid);
                return;
            }
            foreach (var stackCausality in _causalityTable.Values.Where(stackCausality => stackCausality.Name == name))
            {
                _causalityTable.Remove(stackCausality.Guid);
                break;
            }
        }
        internal void MergeWith(CausalityThreadContext context)
        {
            switch (_stacks)
            {
                case null when _activeCausality == null:
                {
                    if (context._activeCausality != null)
                    {
                        AddCausality(context._activeCausality);
                        return;
                    }
                    foreach (var s in context._stacks) 
                        AddCausalityStack(s);
                    return;
                }
                case null:
                    _stacks = new List<CausalityStack>(1);
                    break;
            }

            if (_causalityTable == null) 
                _causalityTable = new Dictionary<Guid, ICausality>();
            
            if (_stacks.Count == 0 && context._activeCausality != null)
            {
                if (_activeCausality.Guid == context._activeCausality.Guid) 
                    return;
                _causalityTable[_activeCausality.Guid] = _activeCausality;
                _causalityTable[context._activeCausality.Guid] = context._activeCausality;
                ((List<CausalityStack>)_stacks).Add(new CausalityStack());
                ((List<CausalityStack>)_stacks)[0].Add(_activeCausality);
                ((List<CausalityStack>)_stacks).Add(new CausalityStack());
                ((List<CausalityStack>)_stacks)[1].Add(context._activeCausality);
                _activeCausality = null;
                return;
            }

            if (_activeCausality != null && context._stacks != null)
            {
                ((List<CausalityStack>)_stacks).Add(new CausalityStack());
                ((List<CausalityStack>)_stacks)[0].Add(_activeCausality);
                _causalityTable[_activeCausality.Guid] = _activeCausality;
                foreach (var stack in context._stacks) AddCausalityStack(stack);
                _activeCausality = null;
                Normalize();
                return;
            }

            if (_stacks.Count <= 0 || context._stacks == null) return;
            
            foreach (var stack in context._stacks) 
                AddCausalityStack(stack);
        }
        private void AddCausalityStack(CausalityStack s)
        {
            if (_stacks == null) 
                _stacks = new List<CausalityStack>(1);
            var stack = new CausalityStack();
            
            if (s.Count <= 0) return;
            
            _causalityTable = new Dictionary<Guid, ICausality>();
            
            foreach (var causality in s.Where(causality => !_causalityTable.ContainsKey(causality.Guid)))
            {
                _causalityTable[causality.Guid] = causality;
                stack.Add(causality);
            }
            if (stack.Count > 0) 
                _stacks.Add(stack);
        }
        internal void PostException(Exception exception)
        {
            if (_activeCausality?.ExceptionPort != null)
            {
                _activeCausality.ExceptionPort.TryPostUnknownType(exception);
                return;
            }
            foreach (var stack in _stacks)
            {
                var causality = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                causality.ExceptionPort?.TryPostUnknownType(exception);
            }
            Normalize();
        }

        private ICollection<CausalityStack> _stacks;
        private ICausality _activeCausality;
        private Dictionary<Guid, ICausality> _causalityTable;
    }
}
