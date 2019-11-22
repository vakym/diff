using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Differentiation
{
    public static class Algebra
    {
        public static Expression<Func<double, double>> Differentiate(Expression<Func<double, double>> function)
        {
            return  Expression.Lambda<Func<double,double>>(BuildSumExpression(Diff(function.Body)),x);
        }

        private static Queue<Expression> Diff(Expression expression)
        {
            var listOfTerms = GetAllTerms(expression);
            var diffListOfTerms = new Queue<Expression>();
            foreach (var term in listOfTerms)
            {
                switch (term.NodeType)
                {
                    case ExpressionType.Constant:
                        diffListOfTerms.Enqueue(GetConstantDiff());
                        break;
                    case ExpressionType.Call:
                        diffListOfTerms.Enqueue(GetMethodDiff(term as MethodCallExpression));
                        break;
                    default:
                        diffListOfTerms.Enqueue(GetParametrDiff(term));
                        break;

                }
            }
            return diffListOfTerms;
        }
        private static ParameterExpression x = Expression.Parameter(typeof(double), "z");
        private static List<Expression> GetAllTerms(Expression expression)
        {
            var terms = new List<Expression>();
            if (expression is BinaryExpression binary
                && expression.NodeType == ExpressionType.Add)
            {
                if (binary.Left is BinaryExpression left)
                {
                    terms.AddRange(GetAllTerms(left));
                }
                else
                {
                    terms.Add(binary.Left);
                }
                if (binary.Right is BinaryExpression rigth)
                {
                    terms.AddRange(GetAllTerms(rigth));
                }
                else
                {
                    terms.Add(binary.Right);
                }
            }
            else
                terms.Add(expression);
            return terms;
        }
        private static Expression BuildSumExpression(Queue<Expression> expressions)
        {
            if(expressions.Count == 1)
                return expressions.Dequeue();

            var left = expressions.Dequeue();
            Expression rigth;
            while (expressions.Count!=0)
            {
                rigth = expressions.Dequeue();
                left = Expression.Add(left, rigth);
            }
            return left;
        }
        #region Rules of differentiation
        //f(x) = c*x^y => f'(x) = c*y*x^y-1  c,y - constant
        private static Expression GetParametrDiff(Expression expression)
        {
            if (expression is ParameterExpression)
                return Expression.Constant(1d);
            else
            {
                var funcData = GetFunctionProperties(expression as BinaryExpression);
                if (funcData.Constant == 0)
                {
                    funcData.Constant = 1;
                }
                return Expression.Multiply(
                            Expression.Constant(funcData.Constant * funcData.Pow),
                            Expression.Call(null, typeof(Math).GetMethod("Pow"), x, Expression.Constant((funcData.Pow - 1))));
            }

            FunctionProperty GetFunctionProperties(BinaryExpression subExpression)
            {
                var funcInfo = new FunctionProperty();
                if(!IsTheEndNode(subExpression.Left))
                {
                    funcInfo += GetFunctionProperties(subExpression.Left as BinaryExpression);
                }
                else
                {
                    if(subExpression.Left is ConstantExpression constant)
                    {
                        funcInfo.Constant += (double)constant.Value;
                    }
                    else
                    {
                        funcInfo.Pow++;
                    }
                }
                if (!IsTheEndNode(subExpression.Right))
                {
                    funcInfo += GetFunctionProperties(subExpression.Right as BinaryExpression);
                }
                else
                {
                    if (subExpression.Right is ConstantExpression constant)
                    {
                        funcInfo.Constant+= (double)constant.Value;
                    }
                    else
                    {
                        funcInfo.Pow++;
                    }
                }
                return funcInfo;
            }
        }

        //f(x) = c => f'(x) = 0 c - constant
        private static Expression GetConstantDiff()
        {
            return Expression.Constant(0d);
        }

        //f(x) = sin(x) => f'(x) = cos(x)
        //f(x) = sin(x^2) => f'(x) = sin(x^2)*2
        private static Expression GetMethodDiff(MethodCallExpression term)
        {
            var method = GetMethod(term.Method);
            if((term.Arguments.First().NodeType !=ExpressionType.Parameter))
            {
                var ex = BuildSumExpression(Diff(term.Arguments.First()));
                var param = term.Arguments.First();
                
                return Expression.Multiply(Expression.Call(null, method, param), ex);
            }
            else
            {
                return Expression.Call(null, method, x);
            }
            MethodInfo GetMethod(MethodInfo currentMethod)
            {
                if(currentMethod.Name=="Sin")
                {
                    return typeof(Math).GetMethod("Cos");
                }
                if(currentMethod.Name == "Cos")
                {
                    return typeof(Math).GetMethod("Sin");
                }
                throw new NotImplementedException($"Differentiation for the {currentMethod.Name} method has not yet been implemented");
            }
        }
        #endregion
        private static bool IsTheEndNode(Expression expression)
        {
            return expression is ConstantExpression || expression is ParameterExpression;
        }
    }

    public class FunctionProperty
    {
        public double Constant { get; set; }
        public double Pow { get; set; }

        public static FunctionProperty operator +(FunctionProperty left,FunctionProperty rigth)
        {
            return new FunctionProperty() { Constant = left.Constant + rigth.Constant, Pow = left.Pow + rigth.Pow };
        }
    }
}
