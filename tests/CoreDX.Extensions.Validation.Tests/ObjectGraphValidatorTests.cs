using CoreDX.Extensions.ComponentModel.DataAnnotations;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CoreDX.Extensions.Validation.Tests
{
    public class ObjectGraphValidatorTests
    {
        [Fact]
        public static async Task TryValidateObjectGraph()
        {
            var instance = new A
            {
                IdA = 1,

                // $.NameA 超长
                NameA = "AAAAAAAAAAQ",
                B = new()
                {
                    // $.B.IdB 超范围（因NameB的连带会显示2个错误）
                    IdB = 200,

                    // $.B.NameB 异步失败（附带对IdB属性名的引用）
                    NameB = "BB"
                }
            };

            instance.Add(new()
            {
                // $[0].IdC 超范围
                IdC = 311,
                B = new()
                {
                    // $[0].B.IdB 超范围（因NameB的连带会显示2个错误）
                    IdB = 400,

                    // $[0].B.NameB 异步失败（附带对IdB属性名的引用）
                    NameB = "CC"
                }
            });

            instance.Add(new() 
            {
                // $[1].IdC 超范围
                IdC = 300

                // $[1].B 必填留空
            });

            // 此处的instance是循环引用，在之前已经验证过
            // $[1].Keys[0]
            instance.ElementAt(1)[instance] = new B
            {
                // $[1].Values[0].IdB 超范围（因NameB的连带会显示2个错误）
                IdB = 500,

                // $[1].Values[0].NameB 异步失败（附带对IdB属性名的引用）
                NameB = "DDDDDDDDDD"
            };

            instance.ElementAt(1)[new A
            {
                // $[1].Keys[1].IdA 超范围
                IdA = 700,
                NameA = "FFFFFFFFFF"
            }] = new B
            {
                // $[1].Values[1].IdB 超范围（因NameB的连带会显示2个错误）
                IdB = 600,
                // $[1].Values[1].NameB 异步失败（附带对IdB属性名的引用）
                NameB = "EEEEEEEEEE",

                // 此处的instance是循环引用，在之前已经验证过
                // $[1].Values[1].A
                A = instance
            };

            // 此处的instance是循环引用，在之前已经验证过
            // $.B.A
            instance.B.A = instance;

            var context = new ValidationContext(instance);
            var resultStore = new ValidationResultStore();
            var result = await ObjectGraphValidator.TryValidateObjectAsync(
                instance,
                context,
                resultStore,
                true,
                type =>
                {
                    if (type == typeof(string) || type == typeof(int))
                        throw new ArgumentException("Called with known built-in type.");

                    return true;
                });

            Assert.False(result);

            // .NET 6.0 以上版本，$ 符号会通过编译特性参数自动替换为参数表达式，此处为变量名 instance

            // $.NameA 超长
            // $.B.IdB 超范围（因NameB的连带会显示2个错误）
            // $.B.NameB 异步失败（附带对IdB属性名的引用）
            // $[0].IdC 超范围
            // $[0].B.IdB 超范围（因NameB的连带会显示2个错误）
            // $[0].B.NameB 异步失败（附带对IdB属性名的引用）
            // $[1].IdC 超范围
            // $[1].B 必填留空
            // $[1].Keys[1].IdA 超范围
            // $[1].Values[0].IdB 超范围（因NameB的连带会显示2个错误）
            // $[1].Values[0].NameB 异步失败（附带对IdB属性名的引用）
            // $[1].Values[1].IdB 超范围（因NameB的连带会显示2个错误）
            // $[1].Values[1].NameB 异步失败（附带对IdB属性名的引用）
            Assert.Equal(13, resultStore.Count());

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                TryValidateObject(instance, new ValidationResultStore(), AsyncValidationBehavior.Throw);
            });

            Assert.Equal("Async validation called synchronously.", exception.Message);

            var resultStore2 = new ValidationResultStore();
            TryValidateObject(instance, resultStore2, AsyncValidationBehavior.TrySynchronously);

            Assert.Equal(13, resultStore2.Count());

            var resultStore3 = new ValidationResultStore();
            TryValidateObject(instance, resultStore3, AsyncValidationBehavior.Ignore);

            Assert.Equal(9, resultStore3.Count());

            static bool TryValidateObject(object instance, ValidationResultStore resultStore, AsyncValidationBehavior behavior)
            {
                return ObjectGraphValidator.TryValidateObject(
                    instance,
                    new ValidationContext(instance),
                    resultStore,
                    behavior,
                    true,
                    type =>
                    {
                        if (type == typeof(string) || type == typeof(int))
                            throw new ArgumentException("Called with known built-in type.");

                        return true;
                    });
            }
        }

        [Fact]
        public static async Task TryValidateObjectGraphThrowsIfValidationContextUsedMultiTimes()
        {
            var instance = new object();
            var context = new ValidationContext(instance);

            var result = await ObjectGraphValidator.TryValidateObjectAsync(instance, context, null, true);
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => ObjectGraphValidator.TryValidateObjectAsync(instance, context, null, true).AsTask());

            Assert.True(result);
            Assert.Equal("The validation context has already been used. (Parameter 'validationContext')", exception.Message);
        }
    }

    #region Test classes

    public class A : ICollection<C>
    {
        private readonly List<C> _list = [];

        [Range(0, 100)]
        public int IdA { get; set; }

        [Required, MaxLength(10)]
        public string NameA { get; set; }

        public B B { get; set; }

        public int Count => ((ICollection<C>)_list).Count;

        public bool IsReadOnly => ((ICollection<C>)_list).IsReadOnly;

        public void Add(C item) => ((ICollection<C>)_list).Add(item);

        public void Clear() => ((ICollection<C>)_list).Clear();

        public bool Contains(C item) => ((ICollection<C>)_list).Contains(item);

        public void CopyTo(C[] array, int arrayIndex) => ((ICollection<C>)_list).CopyTo(array, arrayIndex);

        public IEnumerator<C> GetEnumerator() => ((IEnumerable<C>)_list).GetEnumerator();

        public bool Remove(C item) => ((ICollection<C>)_list).Remove(item);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();
    }

    public class B
    {
        [Range(0, 100)]
        public int IdB { get; set; }

        [Required, MaxLength(10)]
        [AsyncMemberNamesFail]
        public string NameB { get; set; }

        public A A { get; set; }
    }

    public class C : IDictionary<A, B>
    {
        private readonly Dictionary<A, B> _dict = [];

        public B this[A key] { get => ((IDictionary<A, B>)_dict)[key]; set => ((IDictionary<A, B>)_dict)[key] = value; }

        [Range(0, 100)]
        public int IdC { get; set; }

        [Required]
        public B B { get; set; }

        public ICollection<A> Keys => ((IDictionary<A, B>)_dict).Keys;

        public ICollection<B> Values => ((IDictionary<A, B>)_dict).Values;

        public int Count => ((ICollection<KeyValuePair<A, B>>)_dict).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<A, B>>)_dict).IsReadOnly;

        public void Add(A key, B value)
        {
            ((IDictionary<A, B>)_dict).Add(key, value);
        }

        public void Add(KeyValuePair<A, B> item)
        {
            ((ICollection<KeyValuePair<A, B>>)_dict).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<A, B>>)_dict).Clear();
        }

        public bool Contains(KeyValuePair<A, B> item)
        {
            return ((ICollection<KeyValuePair<A, B>>)_dict).Contains(item);
        }

        public bool ContainsKey(A key)
        {
            return ((IDictionary<A, B>)_dict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<A, B>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<A, B>>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<A, B>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<A, B>>)_dict).GetEnumerator();
        }

        public bool Remove(A key)
        {
            return ((IDictionary<A, B>)_dict).Remove(key);
        }

        public bool Remove(KeyValuePair<A, B> item)
        {
            return ((ICollection<KeyValuePair<A, B>>)_dict).Remove(item);
        }

        public bool TryGetValue(A key, [MaybeNullWhen(false)] out B value)
        {
            return ((IDictionary<A, B>)_dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_dict).GetEnumerator();
        }
    }

    public class AsyncMemberNamesFailAttribute : AsyncValidationAttribute
    {
        protected override ValueTask<ValidationResult> IsValidAsync(object value, ValidationContext validationContext, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new ValidationResult(FormatErrorMessage(validationContext.MemberName), [nameof(B.IdB), nameof(B.NameB)]));
        }
    }

    #endregion
}