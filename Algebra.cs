using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Differentiation
{
    public static class Algebra
    {
        public static Expression<Func<double, double>> Differentiate(Expression<Func<double, double>> function)
        {
            var f = function.Body;
            var x = Expression.Parameter(typeof(double), "x");
            if (f is ConstantExpression) 
                ///f(x) = c => f'(x) = 0
                ///(x) => 0
                return Expression.Lambda<Func<double, double>>(
                    Expression.Constant(0d),x
                    );
            if(f is ParameterExpression)
                ///f(x) = x => f'(x) = 1;
                ///(x) => 1
                return Expression.Lambda<Func<double, double>>(
                    Expression.Constant(1d), x);
            var listOfTerms = GetTerms(f as BinaryExpression);
            var termsCount = listOfTerms.Count;
            do
            {
                termsCount = listOfTerms.Count;
                foreach (var term in listOfTerms)
                {
                    listOfTerms.AddRange(GetTerms(term as BinaryExpression));
                }
            } while (listOfTerms.Count != termsCount);
            var diffListOfTerms = new Queue<Expression>();
            foreach (var term in listOfTerms)
            {
                if(term is BinaryExpression)
                {
                    diffListOfTerms.Enqueue(GetParametrDiff(term as BinaryExpression));
                }
            }
            return BuildLambda(diffListOfTerms,x);
        }

        private static Expression<Func<double,double>> BuildLambda(Queue<Expression> expressions, ParameterExpression x)
        {
            if(expressions.Count == 1)
                return Expression.Lambda<Func<double, double>>(expressions.Dequeue(), x);

            var left = expressions.Dequeue();
            Expression rigth;
            while ((rigth = expressions.Dequeue())!=null)
            {
                left = Expression.Add(left, rigth);
            }
            return Expression.Lambda<Func<double, double>>(left, x);
        }   
        private static List<Expression> GetTerms(BinaryExpression binaryExpression)
        {
            return binaryExpression.NodeType == ExpressionType.Add 
                ? new List<Expression>() { binaryExpression.Left, binaryExpression.Right } 
                : new List<Expression>() { binaryExpression };
        }

        private static Expression GetParametrDiff(BinaryExpression expression)
        {
            var x = Expression.Parameter(typeof(double), "x");
            var funcData = GetFunctionProperties(expression);
            return Expression.Multiply(
                        Expression.Constant(funcData.Constant * funcData.Pow),
                        Expression.Call(null,typeof(Math).GetMethod("Pow"),x,Expression.Constant((funcData.Pow - 1))));
            
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
