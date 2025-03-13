using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using HarmonyLib;
using LuaNt.LuaFunctions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Code = HarmonyLib.Code;
using MethodBody = System.Reflection.MethodBody;

namespace LuaNt;

public class LuaNtFile
{
    public static LuaNtFile operator +(LuaNtFile l, Action r)
    {
        l.AddEvent(r);
        return l;
    }

    public readonly List<Action> Events = new();

    public void AddEvent(Action del)
    {
        if (!del.Method.HasMethodBody()) throw new Exception("They have to have a body!");
        Events.Add(del);
    }


    public readonly string RelativePath;
    public string ModPath;
    public string FullPath => Path.Combine(ModPath, RelativePath);

    private class MethodCall
    {
        public string Operator;
        public MethodCall OperatorLeft;
        public MethodCall OperatorRight;

        public MethodCall(MethodCall right, MethodCall left, string op,Instruction il)
        {
            this.OperatorLeft = left;
            this.OperatorRight = right;
            this.Operator = op;
            this.Instruction = il;
        }
        
        public ILLabel Label;
        public Instruction LabelStart;
        public MethodCall Condition;
        public bool _negateCondition;

        public MethodCall(ILLabel label, Instruction labelStart,Instruction t,MethodCall condition,bool negateCondition=false)
        {
            this.Label = label;
            this.LabelStart = labelStart;
            this.Instruction = t;
            Condition = condition;
            _negateCondition = negateCondition;
        }
        
        
        public MethodCall ArrayValue;
        public MethodCall ArrayIndex;
        public MethodCall Array;

        public int? InitArrayWhere;

        public MethodCall ArraySize;

        
        public MethodCall(MethodCall newArraySize)
        {
            this.ArraySize = newArraySize;
            InvokeChars = "{}";
        }

        public MethodCall(MethodCall arrayValue, MethodCall arrayIndex, MethodCall array)
        {
            this.ArrayValue = arrayValue;
            this.ArrayIndex = arrayIndex;
            this.Array = array;
            //TODO: check if this is creation
            var s = array.ArraySize.Make();
            if (int.TryParse(s, out var v))
            {
                if (InvokeParams.Count < v)
                {
                    InitArrayWhere = v;
                    array.InvokeParams.Add(this);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public int? VariableId;
        public MethodCall VariableSetValue;

        public bool LoadConstant;

        public object ConstantValue;

        public string InvokeChars;
        public MethodBase Method;
        public  Instruction Instruction;
        public List<Instruction> InstructionsRef;
        public List<MethodCall> InvokeParams = new List<MethodCall>();
        public MethodCall Instance;

        public bool IsProperty => Method != null && (Method.Name.Contains("set_") || Method.Name.Contains("get_"));
        public string Name;

        public string Make(bool arInit = false,List<MethodCall> toRemove=null)
        {
            
            if (LabelStart != null)
            {
                var where = $"_{this.Instruction.Offset}_";
                if(Condition==null)
                return $"goto {where}";
                else 
                    return $"if {(_negateCondition?"not":"")} ({Condition.Make()}) then\n\tgoto {where}\nend";
            }
            
            if (OperatorLeft != null && OperatorRight != null)
            {
                return $"{OperatorLeft.Make()} {Operator} {OperatorRight.Make()}";
            }
            if (FakeValue != null)
            {
                return FakeValue;
            }
            if (ArraySize != null)
            {
                Console.WriteLine("debug");
            }
            if (arInit  && ArrayValue != null)
            {
                return ArrayValue.Make();
            }
            StringBuilder sb = new StringBuilder();
            if (Name!=null&&Name.StartsWith("op_"))
            {
                Name = "";
               InvokeChars = "";
            }
            if (Name == ".ctor")
            {
                Name = "";
                if (Method.DeclaringType.GetCustomAttribute<HideLuaNtAttribute>() == null)
                {
                    InvokeChars = "{}";
                    //add type to stuff
                    var t = Method.DeclaringType.Property("type");
                    if (t != null)
                    {
                        var v = t.GetValue(Method.DeclaringType.CreateInstance());
                        if (v != null)
                        {
                            var s = (string)v;
                            if (!s.StartsWith("\"")) s = $"\"{s}\"";
                            InvokeParams.Add(new MethodCall("type",s,null));

                        }
                    }
                }
                else InvokeChars = "";
            }

            if (Instance != null && (toRemove==null|| !toRemove.Contains(Instance) ))
            {
                Instance.Make();

                var inst = Instance.Make();
                sb.Append(inst);
                if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Instance.Name))
                    sb.Append(".");
            }


            if (VariableId != null && !declaredLocal.Contains(Name))
            {
                declaredLocal.Add(Name);
            }

            sb.Append(Name);

            if (this.VariableSetValue != null)
            {
                sb.Append($" = {VariableSetValue.Make()}");
            }

            
            if (Method != null && Method.Name != null && Method.Name.Contains("set_")) sb.Append(" = ");
            if (InvokeChars?.Length == 2)
                sb.Append(InvokeChars[0]);
            if (InvokeParams.Count > 0)
            {
                var pl = InvokeParams.OrderBy(a => a.InitArrayWhere == null ? -1 : InitArrayWhere)
                    .Select(a => a.Make(ArraySize != null,new List<MethodCall>(){this})).ToList();
                if (this.Method != null)
                {
                    var pr = Method.GetParameters();
                    for (int i = 0; i < Math.Min(pl.Count,pr.Length); i++)
                    {
                        if (pr[i].ParameterType == typeof(bool))
                        {
                            if (pl[i] == "1") pl[i] = "true";
                            else if (pl[i] == "0") pl[i] = "false";
                            else throw new Exception("Not true");
                        }
                    }
                }
                sb.Append(string.Join(", ",pl ));
            }
            if (InvokeChars?.Length == 2)
                sb.Append(InvokeChars[1]);
            return sb.ToString();
        }

        public MethodCall(int variableId, string variableName, MethodCall set,Instruction il)
        {
            this.VariableId = variableId;
            this.Name = variableName;
            this.VariableSetValue = set;
            this.Instruction = il;
        }

        public override string ToString()
        {
            if (Instruction != null) return Instruction.ToString();
            else return "grrrr";
        }

        public string FakeValue;
        public MethodCall(string fake, string value,Instruction il)
        {
            this.Instruction = il;
            if (fake != null)
            {
                this.FakeValue = $"{fake} = {value}";
            }
           else this.FakeValue = value;
        }
        public MethodCall(int variableId, string variableName,Instruction il)
        {
            this.Instruction = il;
            this.VariableId = variableId;
            this.Name = variableName;
        }

        public MethodCall(bool loadConstant, Instruction instruction, List<Instruction> instructionsRef,
            object? constantValue = null)
        {
            LoadConstant = loadConstant;
            Instruction = instruction;
            this.InstructionsRef = instructionsRef;
            InvokeChars = "";
            Method = null;
            if (constantValue != null) ConstantValue = constantValue;
            else if (Instruction.OpCode == OpCodes.Ldstr)
            {
                ConstantValue = $"\"{Instruction.Operand.ToString()}\"";
            }

            Name = ConstantValue.ToString();
        }

        public MethodCall(MethodBase method, Instruction instruction, List<Instruction> instructionsRef,
            string invokeChars = "()",bool isBool=false)
        {
            InvokeChars = invokeChars;
            Method = method;
            Instruction = instruction;
            InstructionsRef = instructionsRef;
            Name = Method?.Name?.Replace("get_", "")?.Replace("set_", "");
            if (isBool)
            {
                if (Name == "1") Name = "true";
                else if (Name == "0") Name = "false";
                else
                {
                    throw new Exception("Not true");
                }
            }
            if (IsProperty)
            {
                var pars = method.GetParameters();
                bool normal = true;
                if (method.Name.Contains("get_") && pars.Length > 0) normal = false;
                else if (method.Name.Contains("set_") && pars.Length > 1) normal = false;
                if (normal)
                {
                    InvokeChars = "";
                }
                else
                {
                    Name = "";
                    InvokeChars = "[]";
                }
            }
        }
        
        
       
        

        private static List<FieldInfo> fields =typeof(MethodCall).GetFields(AccessTools.all).ToList().FindAll(a=>a.FieldType==typeof(MethodCall)||a.FieldType==typeof(List<MethodCall>));
        public bool Contains(MethodCall methodCall,int nest=0)
        {
            if (nest > 4) return false;
            foreach (var fieldInfo in fields)
            {
                if (string.IsNullOrEmpty(fieldInfo.Name)) continue;
                if (fieldInfo.FieldType == typeof(MethodCall))
                {
                    var v = (MethodCall)fieldInfo.GetValue(this);
                    if (v == this) continue;
                    if (v != null)
                        if (v == methodCall || (v).Contains(methodCall,nest+1))
                            return true;
                }
                else if (fieldInfo.FieldType == typeof(List<MethodCall>))
                {
                    var l = ((List<MethodCall>)fieldInfo.GetValue(this));
                    if(l!=null)
                        if (l.Contains(methodCall) || l.Any(a=>a!=this&& a.Contains(methodCall,nest+1))) return true;
                }
            }


            return false;
        }
    }


    private static List<string> declaredLocal = new List<string>();

    public void Compile()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var expr in Events)
        {
            List<Instruction> ilInstructions = null;
            var method = expr.Method;
            using (var h = new ILHook(method, il =>
                   {
                       ilInstructions = il.Instrs.ToList();
                       il.Instrs.Clear();
                       il.IL.Emit(OpCodes.Nop);
                       il.IL.Emit(OpCodes.Ret);
                   }, true))
            {
                expr();
            }

            for (int i = 0; i < ilInstructions.Count; i++)
            {
                var cur = ilInstructions[i];
                //TURN ON WHEN NEEDED:sb.AppendLine($"--{cur}");
            }

            Stack<MethodCall> methodCalls = new Stack<MethodCall>();
            Stack<MethodCall> stack = new Stack<MethodCall>();
            for (int i = 0; i < ilInstructions.Count; i++)
            {
                var cur = ilInstructions[i];
                var opCode = cur.OpCode;

                int? stloc = null;
                if (opCode == OpCodes.Stloc_0) stloc = 0;
                else if (opCode == OpCodes.Stloc_1) stloc = 1;
                else if (opCode == OpCodes.Stloc_2) stloc = 2;
                else if (opCode == OpCodes.Stloc_3) stloc = 3;
                else if (opCode == OpCodes.Stloc_S)
                {
                    var op = (VariableDefinition)cur.Operand;
                    stloc = op.Index;
                }

                int? ldloc = null;
                if (opCode == OpCodes.Ldloc_0) ldloc = 0;
                else if (opCode == OpCodes.Ldloc_1) ldloc = 1;
                else if (opCode == OpCodes.Ldloc_2) ldloc = 2;
                else if (opCode == OpCodes.Ldloc_3) ldloc = 3;
                else if (opCode == OpCodes.Ldloc_S)
                {
                    var op = (VariableDefinition)cur.Operand;
                    ldloc = op.Index;
                }

                float? ldcr4 = null;
                if (opCode == OpCodes.Ldc_R4) ldcr4 = (float)cur.Operand;

                double? ldcr8 = null;
                if (opCode == OpCodes.Ldc_R8) ldcr8 = (double)cur.Operand;

                int? ldci4 = null;
                if (opCode == OpCodes.Ldc_I4_0) ldci4 = 0;
                else if (opCode == OpCodes.Ldc_I4) ldci4 = (int)cur.Operand;
                else if (opCode == OpCodes.Ldc_I4_S) ldci4 = (SByte)cur.Operand;
                else if (opCode == OpCodes.Ldc_I4_1) ldci4 = 1;
                else if (opCode == OpCodes.Ldc_I4_2) ldci4 = 2;
                else if (opCode == OpCodes.Ldc_I4_3) ldci4 = 3;
                else if (opCode == OpCodes.Ldc_I4_4) ldci4 = 4;
                else if (opCode == OpCodes.Ldc_I4_5) ldci4 = 5;
                else if (opCode == OpCodes.Ldc_I4_6) ldci4 = 6;
                else if (opCode == OpCodes.Ldc_I4_7) ldci4 = 7;
                else if (opCode == OpCodes.Ldc_I4_8) ldci4 = 8;


                bool stelem = false;
                if (opCode == OpCodes.Stelem_Any || opCode == OpCodes.Stelem_I || opCode == OpCodes.Stelem_I1 ||
                    opCode == OpCodes.Stelem_I2 || opCode == OpCodes.Stelem_I4 || opCode == OpCodes.Stelem_I8 ||
                    opCode == OpCodes.Stelem_R4 || opCode == OpCodes.Stelem_R8 ||
                    opCode == OpCodes.Stelem_Ref) stelem = true;

                MethodCall HandleMethodCall(MethodReference metRef)
                {
                    var metNorm = metRef.ToNormal();
                    var m = new MethodCall(metNorm, cur, ilInstructions);
                    List<MethodCall> pars = new List<MethodCall>();
                    var mPar = metNorm.GetParameters();

                    for (int j = 0; j < mPar.Length; j++)
                    {
                        if (stack.Count == 0)
                        {
                            Console.WriteLine("debug");
                            throw new Exception("???");
                        }

                        pars.Add(stack.Pop());
                    }

                    m.InvokeParams = pars;
                    if (!metNorm.IsStatic && !metNorm.IsConstructor)
                    {
                        m.Instance = stack.Pop();
                    }

                    methodCalls.Push(m);
                    if (metNorm is MethodInfo meti)
                        if (meti.ReturnType != typeof(void))
                            stack.Push(m);
                    return m;
                }


                if (opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
                {
                    var metRef = (MethodReference)cur.Operand;
                    HandleMethodCall(metRef);
                }
                else if (opCode == OpCodes.Ldstr)
                {
                    stack.Push(new MethodCall(true, cur, ilInstructions));
                    methodCalls.Push(stack.Peek());
                }
                else if (ldci4 != null)
                {
                    stack.Push(new MethodCall(true, cur, ilInstructions, ldci4));
                    methodCalls.Push(stack.Peek());
                }
                else if (ldcr4 != null)
                {
                    stack.Push(new MethodCall(true, cur, ilInstructions, ldcr4));
                    methodCalls.Push(stack.Peek());
                }
                else if (ldcr8 != null)
                {
                    stack.Push(new MethodCall(true, cur, ilInstructions, ldcr8));
                    methodCalls.Push(stack.Peek());
                }
                else if (stelem)
                {
                    var v = (stack.Pop(), stack.Pop(), stack.Pop());
                    var ar = v.Item3;
                    var index = v.Item2;
                    var val = v.Item1;
                    methodCalls.Push(new MethodCall(val, index, ar));
                }
                else if (opCode == OpCodes.Pop)
                {
                    stack.Pop();
                    methodCalls.Push(new MethodCall(null,null,cur));
                }
                else if (opCode == OpCodes.Ret)
                {
                    Console.WriteLine("Return found!");
                    break;
                }
                else if (opCode == OpCodes.Castclass)
                {
                    //ignore
                }
                else if (stloc != null)
                {
                    if (stack.Count == 0) throw new Exception("???");
                    var v = stack.Pop();
                    methodCalls.Push(new MethodCall(stloc.Value, $"v_{stloc.Value}", v,cur));
                }
                else if (ldloc != null)
                {
                    stack.Push(new MethodCall(ldloc.Value, $"v_{ldloc.Value}",cur));
                    methodCalls.Push(stack.Peek());
                }
                else if (opCode == OpCodes.Newarr)
                {
                    stack.Push(new MethodCall(stack.Pop()));
                    methodCalls.Push(stack.Peek());
                }
                else if (opCode == OpCodes.Newobj)
                {
                    if (cur.Operand is MethodReference met)
                    {
                        var m = HandleMethodCall(met);
                        m.InvokeChars = "{}";
                        stack.Push(m);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    methodCalls.Push(stack.Peek());
                    //stack.Push(new MethodCall(null, cur,ilInstructions,invokeChars:"{}"));
                }
                else if (opCode == OpCodes.Dup)
                {
                    stack.Push(stack.Peek());
                }
                else if (opCode == OpCodes.Br_S)
                {
                    ILLabel where = (ILLabel)cur.Operand;
                    var whereTarget = where.Target;
                    methodCalls.Push(new MethodCall(where,whereTarget,cur,null));
                }
                else if (opCode == OpCodes.Brtrue_S)
                {
                    ILLabel where = (ILLabel)cur.Operand;
                    var whereTarget = where.Target;
                    methodCalls.Push(new MethodCall(where,whereTarget,cur,stack.Pop()));
                }
                else if (opCode == OpCodes.Brfalse_S)
                {
                    ILLabel where = (ILLabel)cur.Operand;
                    var whereTarget = where.Target;
                    methodCalls.Push(new MethodCall(where,whereTarget,cur,stack.Pop(),true));
                }
                else if (opCode == OpCodes.Add)
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var m = new MethodCall(right, left, "+",cur);
                    methodCalls.Push(m);
                    stack.Push(m);
                }
                else if (opCode == OpCodes.Mul)
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var m = new MethodCall(right, left, "*",cur);
                    methodCalls.Push(m);
                    stack.Push(m);
                }
                else if (opCode == OpCodes.Sub)
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var m = new MethodCall(right, left, "-",cur);
                    methodCalls.Push(m);
                    stack.Push(m);
                }
                else if (opCode == OpCodes.Div)
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var m = new MethodCall(right, left, "/",cur);
                    methodCalls.Push(m);
                    stack.Push(m);
                }
                else if (opCode == OpCodes.Clt)
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var m = new MethodCall(right, left, "<",cur);
                    methodCalls.Push(m);
                    stack.Push(m);
                }
                else if (opCode == OpCodes.Cgt)
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var m = new MethodCall(right, left, ">",cur);
                    methodCalls.Push(m);
                    stack.Push(m);
                }
                else if (opCode == OpCodes.Ceq)
                {
                    var right = stack.Pop();
                    var left = stack.Pop();
                    var m = new MethodCall(right, left, "==",cur);
                    methodCalls.Push(m);
                    stack.Push(m);
                }
                else if (opCode == OpCodes.Nop)
                {
                    methodCalls.Push(new MethodCall(null,null,cur));
                }
                else
                    throw new Exception($"Unhandled IL instruction {opCode} {cur}");
            }

            if (stack.Count > 0) throw new Exception("Stack size > 0");

            var methods = methodCalls.ToList();
            methods.Reverse();
            var l = methods.ToList();
            l.RemoveAll(a => methods.ToList().Any(b => b.Contains(a)));
            foreach (var call in l.ToList())
            {
                if (call.Method != null && call.Instance != null && call.Instance.Method != null)
                    if (call.Method.Name.Contains("set_") && call.Instance.Method.Name.Contains(".ctor"))
                    {
                        call.Instance.InvokeParams.Add(call);
                        l.Remove(call);
                    }
            }

            foreach (var call in l.ToList())
            {
                if (call.LabelStart != null)
                {
                    var labelStartmethodIndex = methods.FindIndex(a=>a.Instruction==call.LabelStart);
                    var labelStartmethod = methods[labelStartmethodIndex];
                    while(labelStartmethod.Instruction.OpCode==OpCodes.Nop)labelStartmethod=methods[++labelStartmethodIndex];
                    var index=l.FindIndex(a => a.Contains(labelStartmethod));
                    var last=l.FindLastIndex(a => a.Contains(labelStartmethod));
                    if (index != last) throw new NotImplementedException("!!!");
                    if (index == -1) throw new Exception("wtf");
                    l.Insert(index, new MethodCall(null,$"::_{call.Instruction.Offset}_::",null));
                }
            }



      
            foreach (var call in l)
            {
                var str = call.Make();
                if(!string.IsNullOrEmpty(str))
                  sb.AppendLine(str);
                Console.WriteLine(str);
            }

            Console.WriteLine($"Processing action {expr}");
        }
        foreach (var loc in declaredLocal)
        {
            sb.Insert(0, $"local {loc} = {{}} --premade local\n");
        }
        File.WriteAllText(FullPath, sb.ToString());
    }

    public LuaNtFile(string relativePath)
    {
        RelativePath = relativePath;
    }
}