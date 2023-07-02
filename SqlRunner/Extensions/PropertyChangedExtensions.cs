using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace SqlRunner.Extensions
{
    internal static class PropertyChangedExtensions
    {
        public static void ChangeAndNotify<T>(this PropertyChangedEventHandler handler, ref T field, T value, Expression<Func<T>> memberExpression, Action onChange = null)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException(nameof(memberExpression));
            }
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }
            field = value;

            MemberExpression body = memberExpression.Body as MemberExpression ?? throw new ArgumentException("Lambda must return a property.");
            if (body.Expression is ConstantExpression vmExpression)
            {
                LambdaExpression lambda = Expression.Lambda(vmExpression);
                Delegate vmFunc = lambda.Compile();
                object sender = vmFunc.DynamicInvoke();

                handler?.Invoke(sender, new PropertyChangedEventArgs(body.Member.Name));
            }

            onChange?.Invoke();
        }

        public static void Notify<T>(this PropertyChangedEventHandler handler, Expression<Func<T>> memberExpression, Action onChange = null)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException(nameof(memberExpression));
            }

            MemberExpression body = memberExpression.Body as MemberExpression ?? throw new ArgumentException("Lambda must return a property.");
            if (body.Expression is ConstantExpression vmExpression)
            {
                LambdaExpression lambda = Expression.Lambda(vmExpression);
                Delegate vmFunc = lambda.Compile();
                object sender = vmFunc.DynamicInvoke();

                handler?.Invoke(sender, new PropertyChangedEventArgs(body.Member.Name));
            }

            onChange?.Invoke();
        }
    }
}
