﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace LyricsEngine.LyricsSites
{
    public static class LyricsSiteFactory
    {
        // Strings
        private const string NoPaymentprocessorHasBeenRegisteredWithTheIdentifier = "No PaymentProcessor has been registered with the identifier: ";
        private const string IdentifierCanNotBeNullOrEmpty = "identifier can not be null or empty";
        private const string Createinstance = "CreateInstance";

        private static readonly Type ClassType = typeof (AbstractSite);
        private static readonly Type[] ConstructorArgs = new[] {typeof (string), typeof (string), typeof(WaitHandle), typeof (int)};

        private static readonly Dictionary<string, Type> ClassRegistry = new Dictionary<string, Type>();
        private static readonly Dictionary<string, ConstructorDelegate> ClassConstructors = new Dictionary<string, ConstructorDelegate>();

        private delegate AbstractSite ConstructorDelegate(string artist, string title, WaitHandle mEventStopSiteSearches, int timeLimit);

        static LyricsSiteFactory()
        {
            var lyricSites = from b in Assembly.GetExecutingAssembly().GetTypes()
                                    where b.IsSubclassOf(ClassType)
                                    select b;

            foreach (var type in lyricSites)
            {
                ClassRegistry.Add(type.Name, type);
            }
        }

        public static List<string> LyricsSitesNames()
        {
            return new List<string>(ClassRegistry.Keys);
        }

        public static AbstractSite Create(string identifier, string artist, string title, WaitHandle mEventStopSiteSearches, int timeLimit)
        {
            if (String.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException(IdentifierCanNotBeNullOrEmpty, identifier);
            }
            if (!ClassRegistry.ContainsKey(identifier))
            {
                throw new ArgumentException(NoPaymentprocessorHasBeenRegisteredWithTheIdentifier + identifier);
            }
            return Create(ClassRegistry[identifier], artist, title, mEventStopSiteSearches, timeLimit);
        }

        private static AbstractSite Create(Type type, string artist, string title, WaitHandle mEventStopSiteSearches, int timeLimit)
        {
            ConstructorDelegate del;

            if (ClassConstructors.TryGetValue(type.Name, out del))
            {
                return del(artist, title, mEventStopSiteSearches, timeLimit);
            }

            var dynamicMethod = new DynamicMethod(Createinstance, type, ConstructorArgs, ClassType);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var constructorInfo = type.GetConstructor(ConstructorArgs);
            if (constructorInfo == null)
            {
                throw new NoNullAllowedException("constructorInfo");
            }

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Ldarg_3);
            ilGenerator.Emit(OpCodes.Newobj, constructorInfo);
            ilGenerator.Emit(OpCodes.Ret);

            del = (ConstructorDelegate) dynamicMethod.CreateDelegate(typeof (ConstructorDelegate));
            ClassConstructors.Add(type.Name, del);
            return del(artist, title, mEventStopSiteSearches, timeLimit);
        }
    }
}