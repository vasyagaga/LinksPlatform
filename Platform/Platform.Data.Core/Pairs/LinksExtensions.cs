﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Platform.Data.Core.Exceptions;
using Platform.Data.Core.Sequences;
using Platform.Helpers;
using Platform.Helpers.Collections;

namespace Platform.Data.Core.Pairs
{
    public static class LinksExtensions
    {
        public static readonly LinksConstants<bool, ulong, int> Constants = Default<LinksConstants<bool, ulong, int>>.Instance;

        public static void UseUnicode(this ILinks<ulong> links)
        {
            new UnicodeMap(links).Init();
        }

        public static void RunRandomCreations<T>(this ILinks<T> links, long amountOfCreations)
        {
            for (long i = 0; i < amountOfCreations; i++)
            {
                Integer<T> source = RandomHelpers.DefaultFactory.NextUInt64((Integer<T>)links.Count());
                Integer<T> target = RandomHelpers.DefaultFactory.NextUInt64((Integer<T>)links.Count());

                links.CreateAndUpdate(source, target);
            }
        }

        public static void RunRandomSearches<T>(this ILinks<T> links, long amountOfSearches)
        {
            for (long i = 0; i < amountOfSearches; i++)
            {
                Integer<T> source = RandomHelpers.DefaultFactory.NextUInt64(1, (Integer<T>)links.Count());
                Integer<T> target = RandomHelpers.DefaultFactory.NextUInt64(1, (Integer<T>)links.Count());

                links.SearchOrDefault(source, target);
            }
        }

        public static void RunRandomDeletions<T>(this ILinks<T> links, long amountOfDeletions)
        {
            var min = (ulong)amountOfDeletions > (Integer<T>)links.Count() ? 1 : (Integer<T>)links.Count() - (ulong)amountOfDeletions;

            for (long i = 0; i < amountOfDeletions; i++)
            {
                Integer<T> link = RandomHelpers.DefaultFactory.NextUInt64(min, (Integer<T>)links.Count());
                links.Delete(link);
                if ((Integer<T>)links.Count() < min)
                    break;
            }
        }

        /// <remarks>
        /// TODO: Возможно есть очень простой способ это сделать.
        /// (Например просто удалить файл, или изменить его размер таким образом,
        /// чтобы удалился весь контент)
        /// Например через _header->AllocatedLinks в LinksMemoryManager
        /// </remarks>
        public static void DeleteAll(this ILinks<ulong> links)
        {
            for (var i = links.Count(); i > 0; i--)
            {
                links.Delete(i);
                if (links.Count() != i - 1)
                    i = links.Count();
            }
        }

        public static ulong First(this ILinks<ulong> links)
        {
            ulong firstLink = 0;

            if (links.Count() == 0)
                throw new Exception("В базе данных нет связей.");

            links.Each(Constants.Any, Constants.Any, x =>
            {
                firstLink = x;
                return false;
            });

            if (firstLink == 0)
                throw new Exception("В процессе поиска по базе данных не было найдено связей.");

            return firstLink;
        }

        public static void EnsureEachLinkExists(this ILinks<ulong> links, params ulong[] sequence)
        {
            if (sequence == null)
                return;

            for (var i = 0; i < sequence.Length; i++)
                if (!links.Exists(sequence[i]))
                    throw new ArgumentLinkDoesNotExistsException<ulong>(sequence[i],
                                                                        $"sequence[{i}]");
        }

        public static void EnsureEachLinkIsAnyOrExists(this ILinks<ulong> links, params ulong[] sequence)
        {
            if (sequence == null)
                return;

            for (var i = 0; i < sequence.Length; i++)
                if (sequence[i] != Constants.Any && !links.Exists(sequence[i]))
                    throw new ArgumentLinkDoesNotExistsException<ulong>(sequence[i],
                                                                        $"sequence[{i}]");
        }

        public static bool AnyLinkIsAny(this ILinks<ulong> links, params ulong[] sequence)
        {
            if (sequence == null)
                return false;

            for (var i = 0; i < sequence.Length; i++)
                if (sequence[i] == Constants.Any) return true;

            return false;
        }

        public static string FormatStructure(this ILinks<ulong> links, ulong linkIndex, Func<UInt64Link, bool> isElement, bool renderIndex = false)
        {
            var sb = new StringBuilder();

            links.AppendStructure(sb, linkIndex, isElement, renderIndex);

            return sb.ToString();
        }

        public static void AppendStructure(this ILinks<ulong> links, StringBuilder sb, ulong linkIndex, Func<UInt64Link, bool> isElement, bool renderIndex = false)
        {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));

            if (linkIndex == Constants.Null || linkIndex == Constants.Any || linkIndex == Constants.Itself)
                return;

            sb.Append('(');

            if (links.Exists(linkIndex))
            {
                var link = new UInt64Link(links.GetLink(linkIndex));

                if (renderIndex)
                {
                    sb.Append(link.Index);
                    sb.Append(':');
                }

                if (link.Source == link.Index)
                    sb.Append(link.Index);
                else
                {
                    var source = new UInt64Link(links.GetLink(link.Source));
                    if (isElement(source))
                        sb.Append(source.Index);
                    else
                        links.AppendStructure(sb, source.Index, isElement, renderIndex);
                }

                sb.Append(' ');

                if (link.Target == link.Index)
                    sb.Append(link.Index);
                else
                {
                    var target = new UInt64Link(links.GetLink(link.Target));
                    if (isElement(target))
                        sb.Append(target.Index);
                    else
                        links.AppendStructure(sb, target.Index, isElement, renderIndex);
                }
            }
            else
                sb.Append(linkIndex);

            sb.Append(')');
        }

        public static bool IsInnerReference<T>(this ILinks<T> links, T reference)
        {
            var constants = links.Constants;
            return MathHelpers.GreaterOrEqualThan(constants.MinPossibleIndex, reference) && MathHelpers.LessOrEqualThan(reference, constants.MaxPossibleIndex);
        }

        /// <summary>
        /// Возвращает индекс начальной (Source) связи для указанной связи.
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="link">Индекс связи.</param>
        /// <returns>Индекс начальной связи для указанной связи.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSource<T>(this ILinks<T> links, T link)
        {
            return links.GetLink(link)[Constants.SourcePart];
        }

        /// <summary>
        /// Возвращает индекс конечной (Target) связи для указанной связи.
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="link">Индекс связи.</param>
        /// <returns>Индекс конечной связи для указанной связи.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetTarget<T>(this ILinks<T> links, T link)
        {
            return links.GetLink(link)[Constants.TargetPart];
        }

        /// <summary>
        /// Возвращает части-значения для связи с указанным индексом.
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="link">Индекс связи.</param>
        /// <returns>Уникальную связь.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<TLink> GetLink<TLink>(this ILinks<TLink> links, TLink link)
        {
            var linkPartsSetter = new Setter<IList<TLink>>();
            links.Each(linkPartsSetter.SetAndReturnTrue, link);
            return linkPartsSetter.Result;
        }

        /// <summary>
        /// Выполняет проход по всем связям, соответствующим шаблону, вызывая обработчик (handler) для каждой подходящей связи.
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="source">Значение, определяющее соответствующие шаблону связи. (Constants.Null - 0-я связь, обозначающая ссылку на пустоту в качестве начала, Constants.Any - любое начало, 1..∞ конкретное начало)</param>
        /// <param name="target">Значение, определяющее соответствующие шаблону связи. (Constants.Null - 0-я связь, обозначающая ссылку на пустоту в качестве конца, Constants.Any - любой конец, 1..∞ конкретный конец)</param>
        /// <param name="handler">Обработчик каждой подходящей связи.</param>
        /// <returns>True, в случае если проход по связям не был прерван и False в обратном случае.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Each<TLink>(this ILinks<TLink> links, TLink source, TLink target, Func<TLink, bool> handler)
        {
            var constants = links.Constants;
            return links.Each(link => handler(link[constants.IndexPart]), constants.Any, source, target);
        }

        /// <summary>
        /// Выполняет проход по всем связям, соответствующим шаблону, вызывая обработчик (handler) для каждой подходящей связи.
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="source">Значение, определяющее соответствующие шаблону связи. (Constants.Null - 0-я связь, обозначающая ссылку на пустоту в качестве начала, Constants.Any - любое начало, 1..∞ конкретное начало)</param>
        /// <param name="target">Значение, определяющее соответствующие шаблону связи. (Constants.Null - 0-я связь, обозначающая ссылку на пустоту в качестве конца, Constants.Any - любой конец, 1..∞ конкретный конец)</param>
        /// <param name="handler">Обработчик каждой подходящей связи.</param>
        /// <returns>True, в случае если проход по связям не был прерван и False в обратном случае.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Each<TLink>(this ILinks<TLink> links, TLink source, TLink target, Func<IList<TLink>, bool> handler)
        {
            return links.Each(handler, links.Constants.Any, source, target);
        }

        /// <summary>
        /// Выполняет проход по всем связям, соответствующим шаблону, вызывая обработчик (handler) для каждой подходящей связи.
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="handler">Обработчик каждой подходящей связи.</param>
        /// <param name="restrictions">Ограничения на содержимое связей. Каждое ограничение может иметь значения: Constants.Null - 0-я связь, обозначающая ссылку на пустоту, Any - отсутствие ограничения, 1..∞ конкретный адрес связи.</param>
        /// <returns>True, в случае если проход по связям не был прерван и False в обратном случае.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Each<TLink>(this ILinks<TLink> links, Func<IList<TLink>, bool> handler, params TLink[] restrictions)
        {
            return links.Each(handler, restrictions);
        }

        /// <summary>
        /// Возвращает значение, определяющее существует ли связь с указанным индексом в базе данных.
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="link">Индекс проверяемой на существование связи.</param>
        /// <returns>Значение, определяющее существует ли связь.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Exists<T>(this ILinks<T> links, T link)
        {
            return (Integer<T>)links.Count(link) > 0;
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureLinkExists<T>(this ILinks<T> links, T link)
        {
            if (!links.Exists(link))
                throw new ArgumentLinkDoesNotExistsException<T>(link);
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureLinkExists<T>(this ILinks<T> links, T link, string argumentName)
        {
            if (!links.Exists(link))
                throw new ArgumentLinkDoesNotExistsException<T>(link, argumentName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureInnerReferenceExists<T>(this ILinks<T> links, T reference, string argumentName)
        {
            if (links.IsInnerReference(reference) && !links.Exists(reference))
                throw new ArgumentLinkDoesNotExistsException<T>(reference, argumentName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureInnerReferenceExists<T>(this ILinks<T> links, IList<T> restrictions, string argumentName)
        {
            for (int i = 0; i < restrictions.Count; i++)
                links.EnsureInnerReferenceExists(restrictions[0], argumentName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureLinkIsAnyOrExists<T>(this ILinks<T> links, IList<T> restrictions)
        {
            for (int i = 0; i < restrictions.Count; i++)
                links.EnsureLinkIsAnyOrExists(restrictions[0], nameof(restrictions));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureLinkIsAnyOrExists<T>(this ILinks<T> links, T link, string argumentName)
        {
            if (!Equals(link, links.Constants.Any) && !links.Exists(link))
                throw new ArgumentLinkDoesNotExistsException<T>(link, argumentName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureLinkIsItselfOrExists<T>(this ILinks<T> links, T link, string argumentName)
        {
            if (!Equals(link, links.Constants.Itself) && !links.Exists(link))
                throw new ArgumentLinkDoesNotExistsException<T>(link, argumentName);
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Exists<T>(this ILinks<T> links, T source, T target)
        {
            var constants = Default<LinksConstants<bool, T, int>>.Instance;
            return (Integer<T>)links.Count(constants.Any, source, target) > 0;
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureDoesNotExists<T>(this ILinks<T> links, T source, T target)
        {
            if (links.Exists(source, target))
                throw new LinkWithSameValueAlreadyExistsException();
        }

        /// <param name="links">Хранилище связей.</param>
        public static ulong DependenciesCount<T>(this ILinks<T> links, T link)
        {
            var constants = links.Constants;

            var values = links.GetLink(link);

            ulong referencesAsSource = (Integer<T>)links.Count(constants.Any, link, constants.Any);

            if (Equals(values[Constants.SourcePart], link))
                referencesAsSource--;

            ulong referencesAsTarget = (Integer<T>)links.Count(constants.Any, constants.Any, link);

            if (Equals(values[Constants.TargetPart], link))
                referencesAsTarget--;

            return referencesAsSource + referencesAsTarget;
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DependenciesExist<T>(this ILinks<T> links, T link)
        {
            return links.DependenciesCount(link) > 0;
        }

        /// <param name="links">Хранилище связей.</param>
        public static void EnsureNoDependencies<T>(this ILinks<T> links, T link)
        {
            if (links.DependenciesExist(link))
                throw new ArgumentLinkHasDependenciesException<T>(link);
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals<T>(this ILinks<T> links, T link, T source, T target)
        {
            var values = links.GetLink(link);
            return Equals(values[Constants.SourcePart], source) && Equals(values[Constants.TargetPart], target);
        }

        /// <summary>
        /// Выполняет поиск связи с указанными Source (началом) и Target (концом).
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="source">Индекс связи, которая является началом для искомой связи.</param>
        /// <param name="target">Индекс связи, которая является концом для искомой связи.</param>
        /// <returns>Индекс искомой связи с указанными Source (началом) и Target (концом).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SearchOrDefault<T>(this ILinks<T> links, T source, T target)
        {
            var setter = new Setter<T>();
            links.Each(source, target, setter.SetAndReturnFalse);

            var link = setter.Result;

            if (!Equals(link, default(T)))
                return links.Equals(link, source, target) ? link : default(T);

            return link;
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreatePoint<T>(this ILinks<T> links)
        {
            var link = links.Create();
            return links.Update(link, link, link);
        }

        /// <param name="links">Хранилище связей.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreateAndUpdate<T>(this ILinks<T> links, T source, T target) => links.Update(links.Create(), source, target);

        /// <summary>
        /// Обновляет связь с указанными началом (Source) и концом (Target)
        /// на связь с указанными началом (NewSource) и концом (NewTarget).
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="link">Индекс обновляемой связи.</param>
        /// <param name="newSource">Индекс связи, которая является началом связи, на которую выполняется обновление.</param>
        /// <param name="newTarget">Индекс связи, которая является концом связи, на которую выполняется обновление.</param>
        /// <returns>Индекс обновлённой связи.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TLink Update<TLink>(this ILinks<TLink> links, TLink link, TLink newSource, TLink newTarget) => links.Update(new[] { link, newSource, newTarget });

        /// <summary>
        /// Обновляет связь с указанными началом (Source) и концом (Target)
        /// на связь с указанными началом (NewSource) и концом (NewTarget).
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="restrictions">Ограничения на содержимое связей. Каждое ограничение может иметь значения: Constants.Null - 0-я связь, обозначающая ссылку на пустоту, Itself - требование установить ссылку на себя, 1..∞ конкретный адрес другой связи.</param>
        /// <returns>Индекс обновлённой связи.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TLink Update<TLink>(this ILinks<TLink> links, params TLink[] restrictions)
        {
            return links.Update(restrictions);
        }

        /// <param name="links">Хранилище связей.</param>
        public static void EnsureCreated<T>(this ILinks<T> links, params T[] addresses)
        {
            links.EnsureCreated(links.Create, addresses);
        }

        /// <param name="links">Хранилище связей.</param>
        public static void EnsurePointsCreated<T>(this ILinks<T> links, params T[] addresses)
        {
            links.EnsureCreated(links.CreatePoint, addresses);
        }

        /// <param name="links">Хранилище связей.</param>
        public static void EnsureCreated<T>(this ILinks<T> links, Func<T> creator, params T[] addresses)
        {
            var constants = links.Constants;

            var nonExistentAddresses = new HashSet<ulong>(addresses.Where(x => !links.Exists(x)).Select(x => (ulong)(Integer<T>)x));
            if (nonExistentAddresses.Count > 0)
            {
                var max = nonExistentAddresses.Max();

                // TODO: Эту верхнюю границу нужно разрешить переопределять (проверить применяется ли эта логика)
                max = Math.Min(max, (Integer<T>)constants.MaxPossibleIndex);

                var createdLinks = new List<T>();

                T createdLink;
                while (!Equals(createdLink = creator(), (T)(Integer<T>)max))
                    createdLinks.Add(createdLink);

                for (var i = 0; i < createdLinks.Count; i++)
                {
                    if (!nonExistentAddresses.Contains((Integer<T>)createdLinks[i]))
                        links.Delete(createdLinks[i]);
                }
            }
        }

        /// <summary>
        /// Создаёт связь (если она не существовала), либо возвращает индекс существующей связи с указанными Source (началом) и Target (концом).
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="source">Индекс связи, которая является началом на создаваемой связи.</param>
        /// <param name="target">Индекс связи, которая является концом для создаваемой связи.</param>
        /// <returns>Индекс связи, с указанным Source (началом) и Target (концом)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SearchOrCreate<T>(this ILinks<T> links, T source, T target)
        {
            var link = links.SearchOrDefault(source, target);
            if (Equals(link, default(T)))
                link = links.CreateAndUpdate(source, target);
            return link;
        }

        /// <summary>
        /// Обновляет связь с указанными началом (Source) и концом (Target)
        /// на связь с указанными началом (NewSource) и концом (NewTarget).
        /// </summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="source">Индекс связи, которая является началом обновляемой связи.</param>
        /// <param name="target">Индекс связи, которая является концом обновляемой связи.</param>
        /// <param name="newSource">Индекс связи, которая является началом связи, на которую выполняется обновление.</param>
        /// <param name="newTarget">Индекс связи, которая является концом связи, на которую выполняется обновление.</param>
        /// <returns>Индекс обновлённой связи.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T UpdateOrCreateOrGet<T>(this ILinks<T> links, T source, T target, T newSource, T newTarget)
        {
            var link = links.SearchOrDefault(source, target);
            if (Equals(link, default(T)))
                return links.CreateAndUpdate(newSource, newTarget);
            if (Equals(newSource, source) && Equals(newTarget, target))
                return link;
            return links.Update(link, newSource, newTarget);
        }

        /// <summary>Удаляет связь с указанными началом (Source) и концом (Target).</summary>
        /// <param name="links">Хранилище связей.</param>
        /// <param name="source">Индекс связи, которая является началом удаляемой связи.</param>
        /// <param name="target">Индекс связи, которая является концом удаляемой связи.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeleteOrNothing<T>(this ILinks<T> links, T source, T target)
        {
            var link = links.SearchOrDefault(source, target);
            if (!Equals(link, default(T)))
            {
                links.Delete(link);
                return link;
            }
            return default(T);
        }

        // Replace one link with another (replaced link is deleted, children are updated or deleted), it is actually merge operation
        public static T Merge<T>(this ILinks<T> links, T linkIndex, T newLink)
        {
            if (Equals(linkIndex, newLink))
                return newLink;

            var constants = links.Constants;
            ulong referencesAsSourceCount = (Integer<T>)links.Count(constants.Any, linkIndex, constants.Any);
            ulong referencesAsTargetCount = (Integer<T>)links.Count(constants.Any, constants.Any, linkIndex);

            var references = ArrayPool.Allocate<T>(referencesAsSourceCount + referencesAsTargetCount);
            var referencesFiller = new ArrayFiller<T>(references);

            links.Each(linkIndex, constants.Any, referencesFiller.AddAndReturnTrue);
            links.Each(constants.Any, linkIndex, referencesFiller.AddAndReturnTrue);

            for (ulong i = 0; i < referencesAsSourceCount; i++)
            {
                var reference = references[i];
                if (Equals(reference, linkIndex)) continue;
                links.Update(new[] { reference, newLink, links.GetTarget(reference) });
            }
            for (var i = (long)referencesAsSourceCount; i < references.Length; i++)
            {
                var reference = references[i];
                if (Equals(reference, linkIndex)) continue;
                links.Update(new[] { reference, links.GetSource(reference), newLink });
            }
            ArrayPool.Free(references);

            links.Delete(linkIndex);

            return newLink;
        }
    }
}