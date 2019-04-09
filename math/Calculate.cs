using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

namespace commonlib.math
{
    /// <summary>
    /// 表达式计算类
    /// 备注：
    /// 对于X值为负数时，计算异常，如 -1+2 应该输入 (0-1)+2
    /// </summary>
    /// <remarks>
    /// - 1 --
    /// 2018.10.18 zhengxin
    /// 优化，增加 <(左移)，>(右移)，&(与)，^(异或)，|(或) 5种运算
    /// 修正原运算优先级提取不正确问题
    /// </remarks>
    public class Calculate
    {
        #region 全局变量
        private static string numFormat = @"((([-+*/%<>&^|(]\-)?\d+)(\.\d+)?(\))?)";//把减号当负数 正则表达式
        private static string charFormat = @"[-+*/%<>&^|()]";//正则表达式
        List<double> LnumTemp = new List<double>();//存放表达式中的部分数字，没有括号只有运算符
        List<string> LcharTemp = new List<string>();//存放表达式中的部分字符，没有括号只有运算符
        Dictionary<string, int> PRI = new Dictionary<string, int>(); //优先级字典
        Stack stack = new Stack();
        #endregion

        #region 计算表达式(+ - * / () [] % << >> & ^ |)等，
        /// <summary>
        /// 主调用函数
        /// </summary>
        /// <param name="expression">输入的字符串表达式</param>
        /// <returns>返回表达式的计算值</returns>
        public double CalExpression(string expression)
        {
            double result = 0;
            PRI.Add("*", 8);
            PRI.Add("/", 8);
            PRI.Add("%", 8);
            PRI.Add("+", 7);
            PRI.Add("-", 7);
            PRI.Add("<", 6);  //左移
            PRI.Add(">", 6);  //右移
            PRI.Add("&", 5);  //与
            PRI.Add("^", 4);  //异或
            PRI.Add("|", 3);  //或
            List<double> Lnum = new List<double>();//存放表达式中的数字
            List<string> Lchara = new List<string>();//存放表达式中的字符
            Regex regex = new Regex(numFormat, RegexOptions.IgnoreCase);
            Match match = regex.Match(expression);
            double numAll = 0;//double类型数据
            string charExpre = expression;//用于运算符匹配的字符串
            int iCount = 0;//匹配字符的个数
            string proTemp = "";//过程变量
            while (match.Success)
            {
                iCount = match.Value.Count();
                proTemp = match.Value.Substring(0, 1);
                if (proTemp == "+" || proTemp == "-" || proTemp == "*"
                 || proTemp == "<" || proTemp == ">" || proTemp == "&" || proTemp == "^" || proTemp == "|"
                 || proTemp == "/" || proTemp == "%" || proTemp == "(")
                {
                    if (match.Value.Substring(iCount - 1, 1) != ")")//*-2
                    {
                        numAll = Convert.ToDouble(match.Value.Remove(0, 1));//去除负号前面的运算符
                        charExpre = expression.Replace(match.Value, proTemp);//将字符串中的有负号的数删除为匹配字符正则表达式
                    }
                    else//(-2) *-2)
                    {
                        numAll = Convert.ToDouble(match.Value.Substring(1, iCount - 2));//去除负号前面的运算符和后面的括号
                        charExpre = expression.Replace(match.Value.Substring(1, iCount - 2), "");
                    }
                }
                else
                {
                    if (match.Value.Substring(iCount - 1, 1) == ")")//匹配正则表达式时包含了右括号需要移除
                    {
                        numAll = Convert.ToDouble(match.Value.Remove(iCount - 1, 1));
                    }
                    else
                    {
                        numAll = Convert.ToDouble(match.Value);
                    }
                }
                Lnum.Add(numAll);
                match = match.NextMatch();
            }
            regex = new Regex(charFormat, RegexOptions.IgnoreCase);
            match = regex.Match(charExpre);
            while (match.Success)
            {
                Lchara.Add(match.Value);//将运算符或者括号添加到字符列表中
                match = match.NextMatch();
            }
            result = ExpreMethod(Lnum, Lchara);
            return result;
        }
        #endregion

        #region + - * / %的运算
        /// <summary>
        /// 简单计算
        /// </summary>
        /// <param name="data1">运算符前面的数</param>
        /// <param name="data2">运算符后面的数</param>
        /// <param name="op">运算符</param>
        /// <returns>运算结果double类型</returns>
        public double cal(double data1, double data2, string op)
        {
            double rel = 0;
            switch (op)
            {
                case "+":
                    rel = data1 + data2;
                    break;
                case "-":
                    rel = data1 - data2;
                    break;
                case "*":
                    rel = data1 * data2;
                    break;
                case "/":
                    try
                    {
                        if (data2 == 0)
                        {
                        }
                        else
                        {
                            rel = data1 / data2;
                        }
                    }
                    catch (Exception ex)
                    {
                        string excu = "";
                        if (data2 == 0)
                        {
                            excu = "除数不能为零!";
                        }
                        else
                        {
                            excu = "未知原因！";
                        }
                        throw new Exception(excu, ex);
                    }
                    break;
                //case '%':

                case "%":
                    rel = data1 % data2;
                    break;
                case "<":
                    rel = (int)data1 << (int)data2;
                    break;
                case ">":
                    rel = (int)data1 >> (int)data2;
                    break;
                case "&":
                    rel = (long)data1 & (long)data2;
                    break;
                case "^":
                    rel = (long)data1 ^ (long)data2;
                    break;
                case "|":
                    rel = (long)data1 | (long)data2;
                    break;

                default:
                    break;
            }
            return rel;
        }
        #endregion

        #region 计算表达式的算法
        /// <summary>
        /// 使用栈进行表达式的计算,拆分为简单表达式然后调用函数计算结果返回
        /// </summary>
        /// <param name="num">表达式中的数字部分</param>
        /// <param name="chara">表达式中的字符</param>
        /// <returns>返回表达时候计算的结果 double</returns>
        public double ExpreMethod(List<double> num, List<string> chara)
        {
            double resSamp = 0;//简单表达式的结果
            int iPRI = PRI.Values.Max();//优先级最高字符的值
            string tempExpre = "";//临时存放括号里面的表达式
            int iTemp = 1;//判断出栈的字符时该进入数字列表还是字符列表中
            int jindex = 0;
            int rParentheses = 0;//右括号的个数
            if (num.Count > 0 && chara.Count > 0)
            {
                for (int index = 0; index < num.Count; index++)
                {
                    rParentheses = 0;
                    if ((jindex == 0) && (chara[jindex] == "(" || chara[jindex] == "["))//添加第一个是不是数字而是括号的情况
                    {
                        jindex++;
                        while ((chara[jindex] == "(") || (chara[jindex] == "["))
                        {
                            //stack.Push(chara[jindex]);
                            jindex++;
                        }
                    }
                    stack.Push(num[index]);
                    if (jindex == chara.Count)//在最后计算不带括号的表达式时，表达式已经入栈
                    {
                        break;
                    }
                    stack.Push(chara[jindex]);
                    jindex++;
                    if (jindex == chara.Count)//最后一个字符入栈
                    {
                        stack.Push(num[++index]);//最后一个数字入栈
                        break;
                    }
                    else//如果遇见连续括号时括号均入栈
                    {
                        while ((chara[jindex] == "(") || (chara[jindex] == "["))
                        {
                            stack.Push(chara[jindex]);
                            jindex++;
                        }
                    }
                    if ((chara[jindex] == ")") || (chara[jindex] == "]"))//遇见右括号先把数据入栈先计算括号里面的值
                    {
                        index++;
                        stack.Push(num[index]);
                    }
                    while ((chara[jindex] == ")") || (chara[jindex] == "]"))
                    {
                        rParentheses++;
                        if (rParentheses > 1)//如果有两个右括号则将计算的结果进栈
                        {
                            stack.Push(resSamp);//遇到多个连续的括号数字先进栈  
                        }
                        string sTemp = Convert.ToString(stack.Pop());
                        //string sTemp = (string)stack.Pop();
                        //LnumTemp.Add(Convert.ToDouble(sTemp));
                        iTemp = 0;//判断出栈的字符是该进入数字列表还是字符列表中
                        LnumTemp.Clear();//在每一次计算括号里面的值时清除列表中的内容
                        LcharTemp.Clear();
                        while ((sTemp != "(") && (sTemp != "["))//将括号中表达式的数据或符号分别添加到相应的列表中
                        {
                            tempExpre += sTemp;
                            if (iTemp % 2 == 0)
                            {
                                LnumTemp.Add(Convert.ToDouble(sTemp));//添加到数字列表中
                            }
                            else
                            {
                                LcharTemp.Add(sTemp);//添加到字符列表中
                            }
                            if (stack.Count > 0)
                            {
                                sTemp = Convert.ToString(stack.Pop());
                            }
                            else
                            {
                                break;
                            }
                            iTemp++;
                        }
                        //这里的tempExpre就是一个简单的只有运算符的表达式
                        //简单表达式的结果计算后并返回一个结果值进栈
                        if (tempExpre != "")
                        {
                            resSamp = SampExpreCal(LnumTemp, LcharTemp);//这里调用计算的函数
                            if (rParentheses > 1)//多个括号则数字栈中的有效数字+1
                            {
                                index++;
                            }
                            num[index] = resSamp;
                            index--;
                            jindex++;
                        }
			            if (jindex == chara.Count)
                        {
                            break;
                        }
                    }//判断是否遇到右括号
                }//将列表中的数字和字符都入栈,之后将简单表达式出栈计算结果返回
                LnumTemp.Clear();
                LcharTemp.Clear();
                int nCount = 0;
                nCount = stack.Count;
                for (int m = 0; m < nCount; m++)
                {
                    if (m % 2 == 0)
                    {
                        LnumTemp.Add((double)stack.Pop());
                    }
                    else
                    {
                        LcharTemp.Add((string)stack.Pop());
                    }
                }
                if (LcharTemp.Count > 0 && LnumTemp.Count > 1)//计算必须有两个数字和运算符
                {
                    resSamp = SampExpreCal(LnumTemp, LcharTemp);//这里调用计算的函数
                }
            }//两个列表中都不为空
            return resSamp;
        }
        #endregion

        #region 计算简单表达式的值
        /// <summary>
        /// 计算只有运算符的简单表达式的值
        /// </summary>
        /// <param name="dList">表达式中的数字部分</param>
        /// <param name="cList">表达式中的运算符</param>
        /// <returns>表达式的值</returns>
        public double SampExpreCal(List<double> dList, List<string> cList)
        {
            double resl = 0;
            int high = 0;//保存优先级最高字符的索引
            int cListLen = 0;//字符列表中运算符的个数
            double iTemp = 0;//临时存储计算结果
            while (true)
            {
                cListLen = cList.Count;
                //循环找到优先级最高的运算符
                if (cListLen == 1)
                {
                    resl = cal(dList[1], dList[0], cList[0]);
                    break;
                }
                else
                {
                    int mt = 0;
                    int[] cListIndex = new int[cListLen];
                    for (int ind = 0; ind < cListLen; ind++)
                    {
                        PRI.TryGetValue(cList[ind], out mt);
                        cListIndex[ind] = mt;
                    }
                    //下面的两个循环是找出对应优先级最大的运算符
                    /*
                    for (int m = cListLen - 1; m >= 1; m--)
                    {
                        for (int n = 0; n < m; n++)
                        {
                            if (cListIndex[n] > cListIndex[m])
                            {
                                high = n;
                            }
                            else if (cListIndex[n] == cListIndex[m])
                            {
                                high = m;
                            }
                            else
                            {
                                high = m;
                            }
                        }
                    }
                    */
                    int level = cListIndex[cListIndex.Length - 1];
                    high = cListIndex.Length - 1;
                    for (int i = cListIndex.Length - 2; i >= 0; i--)
                    {
                        if (cListIndex[i] > level)
                        {
                            high = i;
                            level = cListIndex[i];
                        }
                    }
                    
                    iTemp = cal(dList[high + 1], dList[high], cList[high]);
                    dList[high] = iTemp;
                    dList.RemoveAt(high + 1);
                    cList.RemoveAt(high);
                }
            }//while 循环
            return resl;
        }
        #endregion
    }
}
