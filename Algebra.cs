using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Reflection.Differentiation
{
    public static class Algebra
    {
        public static Expression<Func<double, double>> Differentiate(Expression<Func<double, double>> function)
        {
            x = function.Parameters.First();
            return Expression
                .Lambda<Func<double, double>>(BuildSumExpression(DifferentiateTerms(function.Body)), x);
        }

        private static ParameterExpression x;

        private static Queue<Expression> DifferentiateTerms(Expression expression)
        {
            var listOfTerms = GetAllTerms(expression);
            var diffTerms = new Queue<Expression>();
            foreach (var term in listOfTerms)
            {
                switch (term.NodeType)
                {
                    case ExpressionType.Constant:
                        diffTerms.Enqueue(GetConstantDiff());
                        break;
                    case ExpressionType.Call:
                        diffTerms.Enqueue(GetMethodDiff(term as MethodCallExpression));
                        break;
                    default:
                        diffTerms.Enqueue(GetParametrDiff(term));
                        break;
                }
            }
            return diffTerms;
        }

        private static List<Expression> GetAllTerms(Expression expression)
        {
            var terms = new List<Expression>();
            if (expression is BinaryExpression binaryExpression
                && expression.NodeType == ExpressionType.Add)
            {
                AddExpressionToList(binaryExpression.Left);
                AddExpressionToList(binaryExpression.Right);
            }
            else
                terms.Add(expression);
            return terms;

            void AddExpressionToList(Expression subExpression)
            {
                if (subExpression is BinaryExpression subbinaryExpression)
                {
                    terms.AddRange(GetAllTerms(subbinaryExpression));
                }
                else
                {
                    terms.Add(subExpression);
                }
            }
        }

        private static Expression BuildSumExpression(Queue<Expression> expressions)
        {
            if (expressions.Count == 1)
                return expressions.Dequeue();

            var left = expressions.Dequeue();
            Expression rigth;
            while (expressions.Count != 0)
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
            var functionProperties = GetFunctionProperties(expression as BinaryExpression);
            if (functionProperties.Constant == 0)
            {
                functionProperties.Constant = 1;
            }
            return Expression.Multiply(
                      Expression.Constant(functionProperties.Constant * functionProperties.Pow),
                      Expression.Call(null,
                                      typeof(Math).GetMethod("Pow"),
                                      x,
                                      Expression.Constant((functionProperties.Pow - 1))));

            FunctionProperty GetFunctionProperties(BinaryExpression subExpression)
            {
                var funcInfo = new FunctionProperty();
                if (!IsTheEndNode(subExpression.Left))
                {
                    funcInfo += GetFunctionProperties(subExpression.Left as BinaryExpression);
                }
                else
                {
                    if (subExpression.Left is ConstantExpression constant)
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
                        funcInfo.Constant += (double)constant.Value;
                    }
                    else
                    {
                        funcInfo.Pow++;
                    }
                }
                return funcInfo;
            }
            bool IsTheEndNode(Expression checkExpression)
            {
                return checkExpression is ConstantExpression || checkExpression is ParameterExpression;
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
            var method = GetMethod(term.Method, term.Arguments.ToArray());
            if ((term.Arguments.First().NodeType != ExpressionType.Parameter))
            {
                var ex = BuildSumExpression(DifferentiateTerms(term.Arguments.First()));
                return Expression.Multiply(method, ex);
            }
            else
            {
                return method;
            }
            Expression GetMethod(MethodInfo currentMethod, Expression[] expressions)
            {
                if (currentMethod.Name == "Sin")
                {
                    return Expression.Call(null, typeof(Math).GetMethod("Cos"), expressions);
                }
                if (currentMethod.Name == "Cos")
                {
                    return Expression.Multiply(
                        Expression.Constant(-1d),
                        Expression.Call(null, typeof(Math).GetMethod("Sin"), expressions));
                }
                throw new NotImplementedException($"Differentiation for the {currentMethod.Name}" +
                    $" method has not yet been implemented");
            }
        }
        #endregion
    }

    public class FunctionProperty
    {
        public double Constant { get; set; }
        public double Pow { get; set; }

        public static FunctionProperty operator +(FunctionProperty left, FunctionProperty rigth)
        {
            return new FunctionProperty()
            { Constant = left.Constant + rigth.Constant, Pow = left.Pow + rigth.Pow };
        }
    }
}