﻿using Spear.ProxyGenerator.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Spear.ProxyGenerator.Proxy
{
    public class AsyncProxyGenerator : IDisposable
    {
        private readonly Dictionary<Type, Dictionary<Type, Type>> _proxyTypeCaches;

        private readonly ProxyAssembly _proxyAssembly;

        private readonly MethodInfo _dispatchProxyInvokeMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("Invoke");
        private readonly MethodInfo _dispatchProxyInvokeAsyncMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("InvokeAsync");
        private readonly MethodInfo _dispatchProxyInvokeAsyncTMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("InvokeAsyncT");

        public AsyncProxyGenerator()
        {
            _proxyTypeCaches = new Dictionary<Type, Dictionary<Type, Type>>();
            _proxyAssembly = new ProxyAssembly();
        }


        public object CreateProxy(Type interfaceType, ProxyExecutor executor)
        {
            var proxiedType = GetProxyType(executor.GetType(), interfaceType);
            return Activator.CreateInstance(proxiedType, new ProxyHandler(this));
        }

        private Type GetProxyType(Type baseType, Type interfaceType)
        {
            lock (_proxyTypeCaches)
            {
                if (!_proxyTypeCaches.TryGetValue(baseType, out var interfaceToProxy))
                {
                    interfaceToProxy = new Dictionary<Type, Type>();
                    _proxyTypeCaches[baseType] = interfaceToProxy;
                }

                if (!interfaceToProxy.TryGetValue(interfaceType, out var generatedProxy))
                {
                    generatedProxy = GenerateProxyType(baseType, interfaceType);
                    interfaceToProxy[interfaceType] = generatedProxy;
                }

                return generatedProxy;
            }
        }

        // Unconditionally generates a new proxy type derived from 'baseType' and implements 'interfaceType'
        private Type GenerateProxyType(Type baseType, Type interfaceType)
        {
            // Parameter validation is deferred until the point we need to create the proxy.
            // This prevents unnecessary overhead revalidating cached proxy types.
            var baseTypeInfo = baseType.GetTypeInfo();

            // The interface type must be an interface, not a class
            if (!interfaceType.GetTypeInfo().IsInterface)
            {
                // "T" is the generic parameter seen via the public contract
                throw new ArgumentException($"InterfaceType_Must_Be_Interface, {interfaceType.FullName}", "T");
            }

            // The base type cannot be sealed because the proxy needs to subclass it.
            if (baseTypeInfo.IsSealed)
            {
                // "TProxy" is the generic parameter seen via the public contract
                throw new ArgumentException($"BaseType_Cannot_Be_Sealed, {baseTypeInfo.FullName}", "TProxy");
            }

            // The base type cannot be abstract
            if (baseTypeInfo.IsAbstract)
            {
                throw new ArgumentException($"BaseType_Cannot_Be_Abstract {baseType.FullName}", "TProxy");
            }

            // The base type must have a public default ctor
            //if (!baseTypeInfo.DeclaredConstructors.Any(c => c.IsPublic && c.GetParameters().Length == 0))
            //{
            //    throw new ArgumentException($"BaseType_Must_Have_Default_Ctor {baseType.FullName}", "TProxy");
            //}

            // Create a type that derives from 'baseType' provided by caller
            ProxyBuilder pb = _proxyAssembly.CreateProxy("generatedProxy", baseType);

            foreach (Type t in interfaceType.GetTypeInfo().ImplementedInterfaces)
                pb.AddInterfaceImpl(t);

            pb.AddInterfaceImpl(interfaceType);

            Type generatedProxyType = pb.CreateType();
            return generatedProxyType;
        }

        private ProxyMethodResolverContext Resolve(object[] args)
        {
            PackedArgs packed = new PackedArgs(args);
            MethodBase method = _proxyAssembly.ResolveMethodToken(packed.DeclaringType, packed.MethodToken);
            if (method.IsGenericMethodDefinition)
                method = ((MethodInfo)method).MakeGenericMethod(packed.GenericTypes);

            return new ProxyMethodResolverContext(packed, method);
        }

        public object Invoke(object[] args)
        {
            var context = Resolve(args);

            // Call (protected method) DispatchProxyAsync.Invoke()
            object returnValue = null;
            try
            {
                Debug.Assert(_dispatchProxyInvokeMethod != null);
                returnValue = _dispatchProxyInvokeMethod.Invoke(context.Packed.DispatchProxy,
                                                                       new object[] { context.Method, context.Packed.Args });
                context.Packed.ReturnValue = returnValue;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }

            return returnValue;
        }

        public async Task InvokeAsync(object[] args)
        {
            var context = Resolve(args);

            // Call (protected Task method) NetCoreStackDispatchProxy.InvokeAsync()
            try
            {
                Debug.Assert(_dispatchProxyInvokeAsyncMethod != null);
                await (Task)_dispatchProxyInvokeAsyncMethod.Invoke(context.Packed.DispatchProxy,
                                                                       new object[] { context.Method, context.Packed.Args });
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }

        public async Task<T> InvokeAsync<T>(object[] args)
        {
            var context = Resolve(args);

            // Call (protected Task<T> method) NetCoreStackDispatchProxy.InvokeAsync<T>()
            T returnValue = default(T);
            try
            {
                Debug.Assert(_dispatchProxyInvokeAsyncTMethod != null);
                var genericmethod = _dispatchProxyInvokeAsyncTMethod.MakeGenericMethod(typeof(T));
                returnValue = await (Task<T>)genericmethod.Invoke(context.Packed.DispatchProxy,
                                                                       new object[] { context.Method, context.Packed.Args });
                context.Packed.ReturnValue = returnValue;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
            return returnValue;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
