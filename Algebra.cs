using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Reflection.Differentiation
{
    public static class Algebra
    {
        public static Expression<Func<double, double>> Differentiate(
                                                            Expression<Func<double, double>> function)
        {
            x = function.Parameters.First();
            return function.Body.GetTerms()
                                .DifferentiateTerms()
                                .BuildAddExpression()
                                .CreateLambda(x);
        }

        private static ParameterExpression x;

        private static IEnumerable<Expression> GetTerms(this Expression expression)
        {
            if (expression.NodeType == ExpressionType.Add &&
            expression is BinaryExpression addExpression)
            {
                foreach (var left in GetTerms(addExpression.Left))
                {
                    yield return left;
                }
                foreach (var rigth in GetTerms(addExpression.Right))
                {
                    yield return rigth;
                }
            }
            else
                yield return expression;
        }

        private static IEnumerable<Expression> DifferentiateTerms(this IEnumerable<Expression> terms)
        {
            foreach (var term in terms)
            {
                switch (term.NodeType)
                {
                    case ExpressionType.Constant:
                        yield return GetDerivativeOfConstant();
                        break;
                    case ExpressionType.Call:
                        yield return GetDerivativeOfTrigonometric(term as MethodCallExpression);
                        break;
                    default:
                        yield return GetDerivativeOfParametr(term);
                        break;
                }
            }
        }

        private static Expression BuildAddExpression(this IEnumerable<Expression> differentiatedTerms)
        {
            var listOfDifferentiatedTerms = differentiatedTerms.ToList();
            if (listOfDifferentiatedTerms.Count == 1)
                return listOfDifferentiatedTerms.First();
            var left = listOfDifferentiatedTerms[0];
            for (int i = 1; i < listOfDifferentiatedTerms.Count; i++)
            {
                left = Expression.Add(left, listOfDifferentiatedTerms[i]);
            }
            return left;
        }

        private static Expression<Func<double, double>> CreateLambda(this Expression expression,
                                                                     ParameterExpression parameter)
        {
            return Expression
                .Lambda<Func<double, double>>(expression, parameter);
        }

        #region Rules of differentiation
        //f(x) = c*x^y => f'(x) = c*y*x^y-1  c,y - constant
        private static Expression GetDerivativeOfParametr(Expression function)
        {
            if (function is ParameterExpression)
                return Expression.Constant(1d);
            var functionFeatures = FunctionFeatures.GetFeatures(function as BinaryExpression);
            return Expression.Multiply(
                      Expression.Constant(functionFeatures.Constant * functionFeatures.Pow),
                      Expression.Call(null,
                                      typeof(Math).GetMethod("Pow"),
                                      x,
                                      Expression.Constant((functionFeatures.Pow - 1))));
        }

        //f(x) = c => f'(x) = 0 c - constant
        private static Expression GetDerivativeOfConstant()
        {
            return Expression.Constant(0d);
        }

        //f(x) = sin(x) => f'(x) = cos(x)
        //f(x) = sin(x^2) => f'(x) = sin(x^2)*2
        private static Expression GetDerivativeOfTrigonometric(MethodCallExpression function)
        {
            var derivative = GetDerivative(function.Method, function.Arguments.ToArray());
            if ((function.Arguments.First().NodeType != ExpressionType.Parameter))
            {
                var derivativeOfArgument = function.Arguments.First()
                                                            .GetTerms()
                                                            .DifferentiateTerms()
                                                            .BuildAddExpression();
                return Expression.Multiply(derivative, derivativeOfArgument);
            }
            else
            {
                return derivative;
            }

            Expression GetDerivative(MethodInfo currentMethod, Expression[] arguments)
            {
                if (currentMethod.Name == "Sin")
                {
                    return Expression.Call(null, typeof(Math).GetMethod("Cos"), arguments);
                }
                if (currentMethod.Name == "Cos")
                {
                    return Expression.Multiply(
                        Expression.Constant(-1d),
                        Expression.Call(null, typeof(Math).GetMethod("Sin"), arguments));
                }
                throw new NotImplementedException($"Differentiation for the {currentMethod.Name}" +
                    $" method has not yet been implemented");
            }
        }
        #endregion
    }

    public class FunctionFeatures
    {
        public double Constant { get; private set; }
        public double Pow { get; private set; }

        public static FunctionFeatures operator +(FunctionFeatures left, FunctionFeatures rigth)
        {
            return new FunctionFeatures()
            {
                Constant = left.Constant + rigth.Constant,
                Pow = left.Pow + rigth.Pow
            };
        }

        public static FunctionFeatures GetFeatures(BinaryExpression subExpression)
        {
            var funcFeatures = CalculateFeatures(subExpression);
            if (funcFeatures.Constant == 0)
                funcFeatures.Constant = 1;
            return funcFeatures;
        }

        private static FunctionFeatures CalculateFeatures(BinaryExpression function)
        {
            var funcFeatures = new FunctionFeatures();
            funcFeatures += CheckSubExpression(function.Left);
            funcFeatures += CheckSubExpression(function.Right);

            FunctionFeatures CheckSubExpression(Expression checkExpression)
            {
                var ff = new FunctionFeatures();
                if (!IsTheEndNode(checkExpression))
                {
                    ff += CalculateFeatures(checkExpression as BinaryExpression);
                }
                else
                {
                    if (checkExpression is ConstantExpression constant)
                    {
                        ff.Constant += (double)constant.Value;
                    }
                    else
                    {
                        ff.Pow++;
                    }
                }
                return ff;
            }

            bool IsTheEndNode(Expression checkExpression)
            {
                return checkExpression is ConstantExpression || checkExpression is ParameterExpression;
            }

            return funcFeatures;
        }
    }
}