using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Mono.Cecil;
using LSW.Extension;
using Mono.Cecil.Cil;
using System.IO;

namespace 混淆器
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                textBox1.Text = openFileDialog1.FileName;
        }

        static byte[] encode(string s, byte c)
        {
            byte[] b = System.Text.Encoding.UTF8.GetBytes(s);
            for (int i = 0; i < b.Length; i++)
                b[i] += c;
            return b;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int m_index = 0, f_index = 0, p_index = 0, t_index = 0, r_index = 0;

            var assembly = AssemblyDefinition.ReadAssembly(@textBox1.Text);
            //获取PE
            var pe = LSW.Common.Runtime.GetPEFileKinds(@textBox1.Text);
            //抽取方法定义IL
            //var ass = AssemblyDefinition.ReadAssembly(@"ConsoleApplication2.exe");
            //var lm = ass.MainModule.Types.FirstOrDefault(t => t.Name == "Program").Methods.FirstOrDefault(m => m.Name == "lswr");
            //MethodDefinition md = getlswr(assembly);
            //var mins = md.Body.Instructions[0];

            //foreach (var i in lm.Body.Instructions)
            //{
            //    mworker.InsertBefore(mins, i);
            //}
            //注入方法定义
            //assembly.MainModule.Import (
            //md.DeclaringType = assembly.MainModule.Types[0];
            //md.Module.Import(typeof(System.Reflection.Assembly));

            //assembly.MainModule.Types[0].Methods.Add(md);

            textBox2.ShowMsg("MainModule " + assembly.MainModule.Name);

            //嵌入资源
            EmbeddedResource erTemp = new EmbeddedResource("lsw", ManifestResourceAttributes.Public, new byte[] { 0x01, 0x02, 0x03, 0x04 });
            assembly.MainModule.Resources.Add(erTemp);


            foreach (var module in assembly.Modules)
            {
                textBox2.ShowMsg("Module Name " + module.Name);
                foreach (var type in module.Types)
                {
                    textBox2.ShowMsg("Type Name " + type.FullName);
                    //注入方法定义
                    //md.DeclaringType = type;
                    Random ran = new Random();
                    int c = ran.Next(1, 10);
                    var md = getlswr(assembly, c);
                    type.Methods.Add(md);
                    //枚举方法
                    foreach (var method in type.Methods)
                    {
                        textBox2.ShowMsg("Method " + method.FullName);
                        //注入Main方法
                        if (method.Name == "Main" && cb_Main.Checked)
                        {
                            var ins = method.Body.Instructions[0];
                            var worker = method.Body.GetILProcessor();
                            worker.InsertBefore(ins, worker.Create(OpCodes.Nop));
                            worker.InsertBefore(ins, worker.Create(OpCodes.Ldstr, "这是试用版"));
                            worker.InsertBefore(ins, worker.Create(OpCodes.Call, assembly.MainModule.Import(typeof(MessageBox).GetMethod("Show", new Type[] { typeof(string) }))));
                            worker.InsertBefore(ins, worker.Create(OpCodes.Pop));
                        }
                        //混淆form的Name属性
                        if (method.Name == "InitializeComponent" || method.Name == "lswr" || method.Name == "__ENCAddToList")
                        {
                            var worker = method.Body.GetILProcessor();

                            var list = method.Body.Instructions.Where(i => i.Operand != null && i.Operand.ToString().Contains("System.Windows.Forms.Control::set_Name")).ToList();
                            list.ForEach(i =>
                            {
                                if (cb_delName.Checked)
                                {
                                    List<Instruction> ilist = new List<Instruction>();
                                    //查找前导符
                                    while (i.OpCode.Name != "ldarg.0")
                                    {
                                        ilist.Add(i);
                                        i = i.Previous;
                                    }
                                    ilist.Add(i);

                                    foreach (var it in ilist)
                                    {
                                        worker.Remove(it);
                                    }
                                }
                                else
                                {
                                    i.Previous.Operand = "lsw";
                                }
                            });
                        }
                        //系统方法、关键字、构造器不混淆
                        else if (((!method.IsConstructor && !method.IsRuntime) && (!method.IsRuntimeSpecialName && !method.IsSpecialName)) && ((!method.IsVirtual && !method.IsAbstract) && ((method.Overrides.Count <= 0) && !method.Name.StartsWith("<"))))
                        {
                            //公有方法不混淆
                            if (!method.IsPublic)
                            {
                                method.Name = "lsw" + m_index.ToString();
                                m_index++;
                            }
                        }
                        //字符串变资源
                        //for (int j = 0; j < method.Body.Instructions.Count; j++)
                        //{
                        //    var i = method.Body.Instructions[j];

                        //    if (i.OpCode.Name == "ldstr")
                        //    {
                        //        textBox2.ShowMsg(i.Operand.ToString());
                        //        EmbeddedResource erTmp = new EmbeddedResource("lsw" + m_index.ToString(), ManifestResourceAttributes.Public, encode(i.Operand.ToString(), 3));
                        //        assembly.MainModule.Resources.Add(erTmp);

                        //        i.Operand = "lsw" + m_index.ToString();
                        //        var worker = method.Body.GetILProcessor();
                        //        worker.InsertAfter(i, worker.Create(OpCodes.Call, md));
                        //        m_index++;
                        //        j += 2;
                        //    }
                        //}
                        if (method.Body != null)
                        {
                            var strilist = method.Body.Instructions.Where(i => i.OpCode.Name == "ldstr").ToList();
                            strilist.ForEach(i =>
                                {
                                    textBox2.ShowMsg(i.Operand.ToString());
                                    EmbeddedResource erTmp = new EmbeddedResource("lsw" + (char)r_index, ManifestResourceAttributes.Private, encode(i.Operand.ToString(), (byte)c));
                                    assembly.MainModule.Resources.Add(erTmp);

                                    i.Operand = "lsw" + (char)r_index;
                                    var worker = method.Body.GetILProcessor();
                                    worker.InsertAfter(i, worker.Create(OpCodes.Call, md));

                                    r_index++;
                                });
                        }
                    }


                    //枚举字段
                    foreach (var field in type.Fields)
                    {
                        if (!field.IsPublic)
                        {
                            field.Name = "lsw" + f_index.ToString();
                            f_index++;
                        }
                    }
                    //枚举属性
                    foreach (var property in type.Properties)
                    {
                        if (property.GetMethod != null && property.SetMethod != null && !property.GetMethod.IsPublic && !property.SetMethod.IsPublic)
                        {
                            property.Name = "lsw" + p_index.ToString();
                            p_index++;
                        }
                    }
                    //混淆类名
                    if (pe != System.Reflection.Emit.PEFileKinds.Dll)
                    {
                        if (!type.IsPublic && (((type.Name != "<Module>") && !type.IsRuntimeSpecialName) && (!type.IsSpecialName && !type.Name.Contains("Resources"))) && ((!type.Name.StartsWith("<") && !type.Name.Contains("__"))))
                        {
                            type.Name = "lsw" + t_index.ToString();
                            t_index++;
                        }
                    }
                }
            }
            //保存
            //assembly.Write(@"a.exe", new WriterParameters { WriteSymbols = false });
            if (File.Exists(@textBox1.Text + ".bak"))
                File.Delete(@textBox1.Text + ".bak");
            File.Move(@textBox1.Text, @textBox1.Text + ".bak");
            assembly.Write(@textBox1.Text);
            textBox2.ShowMsg("保存成功！");
        }

        private static MethodDefinition getlswr(AssemblyDefinition assembly, int c)
        {
            MethodDefinition md = new MethodDefinition("lswr", MethodAttributes.Static, assembly.MainModule.TypeSystem.String);
            md.Parameters.Add(new ParameterDefinition(assembly.MainModule.TypeSystem.String));
            //md.Body = lm.Body;
            //assembly.MainModule.Import(typeof(System.Reflection.Assembly));

            //md.MetadataToken = lm.MetadataToken;
            //md.Body.LocalVarToken = lm.Body.LocalVarToken;
            //foreach (var v in lm.Body.Variables)
            //{
            //    md.Body.Variables.Add(v);
            //}
            md.Body.Variables.Add(new VariableDefinition("assembly", assembly.MainModule.Import(typeof(System.Reflection.Assembly))));
            md.Body.Variables.Add(new VariableDefinition("stream", assembly.MainModule.Import(typeof(System.IO.Stream))));
            md.Body.Variables.Add(new VariableDefinition("buffer", assembly.MainModule.Import(typeof(byte[]))));
            md.Body.Variables.Add(new VariableDefinition("num", assembly.MainModule.TypeSystem.Int32));
            VariableDefinition varstr = new VariableDefinition("str", assembly.MainModule.TypeSystem.String);
            md.Body.Variables.Add(varstr);
            VariableDefinition varflag = new VariableDefinition("flag", assembly.MainModule.TypeSystem.Boolean);
            md.Body.Variables.Add(varflag);
            md.Body.Variables.Add(new VariableDefinition("a", assembly.MainModule.Import(typeof(System.Reflection.Assembly))));
            md.Body.Variables.Add(new VariableDefinition("b", assembly.MainModule.Import(typeof(System.IO.Stream))));
            md.Body.Variables.Add(new VariableDefinition("c", assembly.MainModule.Import(typeof(byte[]))));
            md.Body.Variables.Add(new VariableDefinition("d", assembly.MainModule.TypeSystem.Int32));

            var mworker = md.Body.GetILProcessor();
            md.Body.Instructions.Add(mworker.Create(OpCodes.Nop));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Call, assembly.MainModule.Import(typeof(System.Reflection.Assembly).GetMethod("GetExecutingAssembly"))));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_0));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_0));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldarg_0));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Callvirt, assembly.MainModule.Import(typeof(System.Reflection.Assembly).GetMethod("GetManifestResourceStream", new Type[] { typeof(string) }))));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_1));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Nop));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_1));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Callvirt, assembly.MainModule.Import(typeof(System.IO.Stream).GetMethod("get_Length"))));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Conv_Ovf_I));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Newarr, assembly.MainModule.TypeSystem.Byte));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_2));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_1));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_2));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldc_I4_0));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_2));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldlen));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Conv_I4));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Callvirt, assembly.MainModule.Import(typeof(System.IO.Stream).GetMethod("Read", new Type[] { typeof(byte[]), typeof(int), typeof(int) }))));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Pop));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldc_I4_0));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_3));
            var L0046 = mworker.Create(OpCodes.Ldloc_3);
            md.Body.Instructions.Add(mworker.Create(OpCodes.Br_S, L0046));
            var L002d = mworker.Create(OpCodes.Ldloc_2);
            md.Body.Instructions.Add(L002d);
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_3));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldelema, assembly.MainModule.TypeSystem.Byte));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Dup));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldobj, assembly.MainModule.TypeSystem.Byte));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldc_I4, c));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Sub));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Conv_U1));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stobj, assembly.MainModule.TypeSystem.Byte));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_3));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldc_I4_1));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Add));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_3));
            md.Body.Instructions.Add(L0046);
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_2));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldlen));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Conv_I4));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Clt));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_S, varflag));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_S, varflag));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Brtrue_S, L002d));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Call, assembly.MainModule.Import(typeof(System.Text.Encoding).GetMethod("get_UTF8"))));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_2));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Callvirt, assembly.MainModule.Import(typeof(System.Text.Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }))));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_S, varstr));
            var L0073 = mworker.Create(OpCodes.Nop);
            md.Body.Instructions.Add(mworker.Create(OpCodes.Leave_S, L0073));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_1));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldnull));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ceq));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Stloc_S, varflag));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_S, varflag));
            var L0072 = mworker.Create(OpCodes.Endfinally);
            md.Body.Instructions.Add(mworker.Create(OpCodes.Brtrue_S, L0072));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_1));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Callvirt, assembly.MainModule.Import(typeof(System.IDisposable).GetMethod("Dispose"))));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Nop));
            md.Body.Instructions.Add(L0072);
            md.Body.Instructions.Add(L0073);
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ldloc_S, varstr));
            md.Body.Instructions.Add(mworker.Create(OpCodes.Ret));
            return md;
        }
    }
}
