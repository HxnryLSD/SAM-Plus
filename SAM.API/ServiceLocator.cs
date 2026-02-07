/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace SAM.API
{
    /// <summary>
    /// Simple service locator for dependency injection.
    /// Provides centralized access to shared services like HttpClient.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly object _lock = new();
        private static bool _isInitialized = false;

        /// <summary>
        /// Shared HttpClient instance for all network operations.
        /// </summary>
        public static HttpClient HttpClient => GetService<HttpClient>();

        /// <summary>
        /// Initializes services with default implementations.
        /// Call this once at application startup.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized) return;

                // Register default services
                Register<HttpClient>(new HttpClient());

                _isInitialized = true;
                Logger.Info("ServiceLocator initialized");
            }
        }

        /// <summary>
        /// Registers a service instance.
        /// </summary>
        public static void Register<T>(T instance) where T : class
        {
            lock (_lock)
            {
                _services[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
            }
        }

        /// <summary>
        /// Gets a registered service.
        /// </summary>
        public static T GetService<T>() where T : class
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    Initialize();
                }

                if (_services.TryGetValue(typeof(T), out var service))
                {
                    return (T)service;
                }

                throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
            }
        }

        /// <summary>
        /// Tries to get a registered service.
        /// </summary>
        public static bool TryGetService<T>(out T service) where T : class
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    Initialize();
                }

                if (_services.TryGetValue(typeof(T), out var obj))
                {
                    service = (T)obj;
                    return true;
                }

                service = null;
                return false;
            }
        }
    }
}
