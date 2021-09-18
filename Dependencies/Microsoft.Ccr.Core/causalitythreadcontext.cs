using System;
using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    internal class CausalityThreadContext
    {
        public CausalityThreadContext(ICausality causality, ICollection<CausalityStack> stacks)
        {
            ActiveCausality = causality;
            Stacks = stacks;
        }

        public static bool IsEmpty(CausalityThreadContext context)
        {
            return context == null || (context.ActiveCausality == null && context.Stacks == null);
        }

        public ICollection<ICausality> Causalities
        {
            get
            {
                if (ActiveCausality != null)
                {
                    return new ICausality[]
                    {
                        ActiveCausality
                    };
                }
                List<ICausality> list = new List<ICausality>();
                if (Stacks == null)
                {
                    return list;
                }
                foreach (CausalityStack causalityStack in Stacks)
                {
                    if (causalityStack.Count != 0)
                    {
                        list.Add(causalityStack[causalityStack.Count - 1]);
                    }
                }
                return list;
            }
        }

        private void Normalize()
        {
            if (ActiveCausality != null || Stacks == null)
            {
                return;
            }
            int num = 0;
            List<CausalityStack> list = null;
            foreach (CausalityStack causalityStack in Stacks)
            {
                if (causalityStack.Count == 0)
                {
                    if (list == null)
                    {
                        list = new List<CausalityStack>(1);
                    }
                    list.Add(causalityStack);
                }
                num += causalityStack.Count;
            }
            if (list == null && num > 1)
            {
                return;
            }
            if (list != null)
            {
                foreach (CausalityStack item in list)
                {
                    Stacks.Remove(item);
                }
            }
            if (Stacks.Count == 0)
            {
                Stacks = null;
            }
            if (num == 1)
            {
                ActiveCausality = ((List<CausalityStack>)Stacks)[0][0];
                Stacks = null;
            }
        }

        internal void AddCausality(ICausality causality)
        {
            if (CausalityThreadContext.IsEmpty(this))
            {
                ActiveCausality = causality;
                return;
            }
            if (CausalityTable == null)
            {
                CausalityTable = new Dictionary<Guid, ICausality>();
            }
            if (ActiveCausality == null)
            {
                if (Stacks != null)
                {
                    if (CausalityTable.ContainsKey(causality.Guid))
                    {
                        return;
                    }
                    CausalityTable.Add(causality.Guid, causality);
                    foreach (CausalityStack causalityStack in Stacks)
                    {
                        causalityStack.Add(causality);
                    }
                }
                return;
            }
            if (causality.Guid == ActiveCausality.Guid)
            {
                return;
            }
            CausalityTable.Add(ActiveCausality.Guid, ActiveCausality);
            CausalityTable.Add(causality.Guid, causality);
            Stacks = new List<CausalityStack>();
            CausalityStack causalityStack2 = new CausalityStack
            {
                ActiveCausality,
                causality
            };
            ActiveCausality = null;
            Stacks.Add(causalityStack2);
        }

        internal CausalityThreadContext Clone()
        {
            CausalityThreadContext causalityThreadContext = new CausalityThreadContext(ActiveCausality, null);
            if (ActiveCausality != null)
            {
                return causalityThreadContext;
            }
            causalityThreadContext.Stacks = new List<CausalityStack>();
            foreach (CausalityStack collection in Stacks)
            {
                CausalityStack causalityStack = new CausalityStack();
                causalityStack.AddRange(collection);
                causalityThreadContext.Stacks.Add(causalityStack);
            }
            return causalityThreadContext;
        }

        internal bool RemoveCausality(string name, ICausality causality)
        {
            if (ActiveCausality != null && ((causality != null && causality == ActiveCausality) || name == ActiveCausality.Name))
            {
                RemoveFromTable(name, causality);
                ActiveCausality = null;
                return true;
            }
            bool result = false;
            foreach (CausalityStack causalityStack in Stacks)
            {
                foreach (ICausality causality2 in causalityStack)
                {
                    if ((causality != null && causality == causality2) || name == causality2.Name)
                    {
                        result = true;
                        causalityStack.Remove(causality2);
                        RemoveFromTable(name, causality);
                        break;
                    }
                }
            }
            Normalize();
            return result;
        }

        private void RemoveFromTable(string name, ICausality causality)
        {
            if (CausalityTable != null)
            {
                if (causality != null)
                {
                    CausalityTable.Remove(causality.Guid);
                    return;
                }
                foreach (ICausality causality2 in CausalityTable.Values)
                {
                    if (causality2.Name == name)
                    {
                        CausalityTable.Remove(causality2.Guid);
                        break;
                    }
                }
            }
        }

        internal void MergeWith(CausalityThreadContext context)
        {
            if (Stacks == null && ActiveCausality == null)
            {
                if (context.ActiveCausality != null)
                {
                    AddCausality(context.ActiveCausality);
                    return;
                }
                foreach (CausalityStack s in context.Stacks)
                {
                    AddCausalityStack(s);
                }
                return;
            }
            else
            {
                if (Stacks == null)
                {
                    Stacks = new List<CausalityStack>(1);
                }
                if (CausalityTable == null)
                {
                    CausalityTable = new Dictionary<Guid, ICausality>();
                }
                if (Stacks.Count == 0 && context.ActiveCausality != null)
                {
                    if (ActiveCausality.Guid == context.ActiveCausality.Guid)
                    {
                        return;
                    }
                    CausalityTable[ActiveCausality.Guid] = ActiveCausality;
                    CausalityTable[context.ActiveCausality.Guid] = context.ActiveCausality;
                    ((List<CausalityStack>)Stacks).Add(new CausalityStack());
                    ((List<CausalityStack>)Stacks)[0].Add(ActiveCausality);
                    ((List<CausalityStack>)Stacks).Add(new CausalityStack());
                    ((List<CausalityStack>)Stacks)[1].Add(context.ActiveCausality);
                    ActiveCausality = null;
                    return;
                }
                else
                {
                    if (ActiveCausality != null && context.Stacks != null)
                    {
                        ((List<CausalityStack>)Stacks).Add(new CausalityStack());
                        ((List<CausalityStack>)Stacks)[0].Add(ActiveCausality);
                        CausalityTable[ActiveCausality.Guid] = ActiveCausality;
                        foreach (CausalityStack s2 in context.Stacks)
                        {
                            AddCausalityStack(s2);
                        }
                        ActiveCausality = null;
                        Normalize();
                        return;
                    }
                    if (Stacks.Count > 0 && context.Stacks != null)
                    {
                        foreach (CausalityStack s3 in context.Stacks)
                        {
                            AddCausalityStack(s3);
                        }
                    }
                    return;
                }
            }
        }

        private void AddCausalityStack(CausalityStack s)
        {
            if (Stacks == null)
            {
                Stacks = new List<CausalityStack>(1);
            }
            CausalityStack causalityStack = new CausalityStack();
            if (s.Count > 0)
            {
                CausalityTable = new Dictionary<Guid, ICausality>();
                foreach (ICausality causality in s)
                {
                    if (!CausalityTable.ContainsKey(causality.Guid))
                    {
                        CausalityTable[causality.Guid] = causality;
                        causalityStack.Add(causality);
                    }
                }
                if (causalityStack.Count > 0)
                {
                    Stacks.Add(causalityStack);
                }
            }
        }

        internal void PostException(Exception exception)
        {
            if (ActiveCausality != null && ActiveCausality.ExceptionPort != null)
            {
                ActiveCausality.ExceptionPort.TryPostUnknownType(exception);
                return;
            }
            foreach (CausalityStack causalityStack in Stacks)
            {
                ICausality causality = causalityStack[causalityStack.Count - 1];
                causalityStack.RemoveAt(causalityStack.Count - 1);
                if (causality.ExceptionPort != null)
                {
                    causality.ExceptionPort.TryPostUnknownType(exception);
                }
            }
            Normalize();
        }

        private ICollection<CausalityStack> Stacks;

        private ICausality ActiveCausality;

        internal Dictionary<Guid, ICausality> CausalityTable;
    }
}
