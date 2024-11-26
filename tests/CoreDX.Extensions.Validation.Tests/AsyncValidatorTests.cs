using CoreDX.Extensions.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CoreDX.Extensions.Validation.Tests
{
    public class AsyncValidatorTests
    {
        public static readonly ValidationContext s_estValidationContext = new ValidationContext(new object());

        #region TryValidateObject

        [Fact]
        public static async Task TryValidateObjectThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateObject(new object(), validationContext: null, validationResults: null));
            
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateObject(new object(), validationContext: null, validationResults: null, validateAllProperties: false));
        }

        [Fact]
        public static async Task TryValidateObjectThrowsIf_instance_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateObject(null, s_estValidationContext, validationResults: null));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateObject(null, s_estValidationContext, validationResults: null, validateAllProperties: false));
        }

        // TryValidateObjectThrowsIf_instance_does_not_match_ValidationContext_ObjectInstance
        [Fact]
        public static async Task TestTryValidateObjectThrowsIfInstanceNotMatch()
        {
            await AssertExtensions.ThrowsAsync<ArgumentException>("instance", () => AsyncValidator.TryValidateObject(new object(), s_estValidationContext, validationResults: null));
            await AssertExtensions.ThrowsAsync<ArgumentException>("instance", () => AsyncValidator.TryValidateObject(new object(), s_estValidationContext, validationResults: null, validateAllProperties: true));
        }

        [Fact]
        public static async Task TryValidateObject_returns_true_if_no_errors()
        {
            var objectToBeValidated = "ToBeValidated";
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.True(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults: null));
            Assert.True(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults: null, validateAllProperties: true));
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_errors()
        {
            var objectToBeValidated = new ToBeValidated()
            {
                PropertyToBeTested = "Invalid Value",
                PropertyWithRequiredAttribute = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.False(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObject_collection_can_have_multiple_results()
        {
            HasDoubleFailureProperty objectToBeValidated = new HasDoubleFailureProperty();
            ValidationContext validationContext = new ValidationContext(objectToBeValidated);
            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, results, true));
            Assert.Equal(2, results.Count);
        }


        [Fact]
        public static async Task TryValidateObject_collection_can_have_multiple_results_from_type_attributes()
        {
            DoublyInvalid objectToBeValidated = new DoublyInvalid();
            ValidationContext validationContext = new ValidationContext(objectToBeValidated);
            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, results, true));
            Assert.Equal(2, results.Count);
        }

        // TryValidateObject_returns_true_if_validateAllProperties_is_false_and_Required_test_passes_even_if_there_are_other_errors()
        [Fact]
        public static async Task TestTryValidateObjectSuccessEvenWithOtherErrors()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Invalid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.True(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, null, false));

            var validationResults = new List<ValidationResult>();
            Assert.True(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, false));
            Assert.Equal(0, validationResults.Count);
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_validateAllProperties_is_true_and_Required_test_fails()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = null };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.False(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            // cannot check error message - not defined on ret builds
        }

        [Fact]
        public static async Task TryValidateObject_returns_true_if_validateAllProperties_is_true_and_all_attributes_are_valid()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.True(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.True(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(0, validationResults.Count);
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_all_properties_are_valid_but_class_is_invalid()
        {
            var objectToBeValidated = new InvalidToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            Assert.False(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, null, true));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidClassAttribute.IsValid failed for class of type " + typeof(InvalidToBeValidated).FullName, validationResults[0].ErrorMessage);
        }

        [Fact]
        public async Task TryValidateObject_IValidatableObject_Success()
        {
            var instance = new ValidatableSuccess();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.True(await AsyncValidator.TryValidateObject(instance, context, results));
            Assert.Empty(results);
        }

        public class ValidatableSuccess : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new ValidationResult[] { ValidationResult.Success };
            }
        }

        [Fact]
        public async Task TryValidateObject_IValidatableObject_Error()
        {
            var instance = new ValidatableError();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(instance, context, results));
            Assert.Equal("error", Assert.Single(results).ErrorMessage);
        }

        public class ValidatableError : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return new ValidationResult[] { new ValidationResult("error") };
            }
        }

        [Fact]
        public async Task TryValidateObject_IValidatableObject_Null()
        {
            var instance = new ValidatableNull();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.True(await AsyncValidator.TryValidateObject(instance, context, results));
            Assert.Equal(0, results.Count);
        }

        public class ValidatableNull : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return null;
            }
        }

        [Fact]
        public async Task TryValidateObject_RequiredNonNull_Success()
        {
            var instance = new RequiredFailure { Required = "Text" };
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.True(await AsyncValidator.TryValidateObject(instance, context, results));
            Assert.Empty(results);
        }

        [Fact]
        public async Task TryValidateObject_RequiredNull_Error()
        {
            var instance = new RequiredFailure();
            var context = new ValidationContext(instance);

            var results = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(instance, context, results));
            Assert.Contains("Required", Assert.Single(results).ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The SecondPropertyToBeTested field is required.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value",
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_has_unmatched_property_name()
        {
            var objectToBeValidated = new HasMetadataTypeWithUnmatchedProperties()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithUnmatchedProperties), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithUnmatchedProperties));

            var validationResults = new List<ValidationResult>();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal("The associated metadata type for type 'CoreDX.Extensions.Validation.Tests.AsyncValidatorTests+HasMetadataTypeWithUnmatchedProperties' contains the following unknown properties or fields: SecondPropertyToBeTested. Please make sure that the names of these members match the names of the properties on the main type.",
                exception.Message);
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is required.");
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var objectToBeValidated = new HasMetadataTypeWithComplementaryRequirements()
            {
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.");
        }

        [Fact]
        public static async Task TryValidateObject_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var objectToBeValidated = new SelfMetadataType()
            {
                PropertyToBeTested = "Invalid Value",
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateObject(objectToBeValidated, validationContext, validationResults, true));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
        }

        [Fact]
        public static async Task TryValidateObject_for_JObject_does_not_throw()
        {
            var objectToBeValidated = JObject.Parse("{\"Enabled\":true}");
            var results = new List<ValidationResult>();
            Assert.True(await AsyncValidator.TryValidateObject(objectToBeValidated, new ValidationContext(objectToBeValidated), results, true));
            Assert.Empty(results);
        }

        public class RequiredFailure
        {
            [Required]
            public string Required { get; set; }
        }

        #endregion TryValidateObject

        #region ValidateObject

        [Fact]
        public static async Task ValidateObjectThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateObject(new object(), validationContext: null));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateObject(new object(), validationContext: null, validateAllProperties: false));
        }

        [Fact]
        public static async Task ValidateObjectThrowsIf_instance_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateObject(null, s_estValidationContext));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateObject(null, s_estValidationContext, false));
        }

        [Fact]
        public static async Task ValidateObjectThrowsIf_instance_does_not_match_ValidationContext_ObjectInstance()
        {
            await AssertExtensions.ThrowsAsync<ArgumentException>("instance", () => AsyncValidator.ValidateObject(new object(), s_estValidationContext));
            await AssertExtensions.ThrowsAsync<ArgumentException>("instance", () => AsyncValidator.ValidateObject(new object(), s_estValidationContext, true));
        }

        [Fact]
        public static async Task ValidateObject_succeeds_if_no_errors()
        {
            var objectToBeValidated = "ToBeValidated";
            var validationContext = new ValidationContext(objectToBeValidated);
            await AsyncValidator.ValidateObject(objectToBeValidated, validationContext);
            await AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true);
        }

        [Fact]
        public static async Task ValidateObject_throws_ValidationException_if_errors()
        {
            var objectToBeValidated = new ToBeValidated()
            {
                PropertyToBeTested = "Invalid Value",
                PropertyWithRequiredAttribute = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        // ValidateObject_returns_true_if_validateAllProperties_is_false_and_Required_test_passes_even_if_there_are_other_errors
        [Fact]
        public static async Task TestValidateObjectNotThrowIfvalidateAllPropertiesFalse()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Invalid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            await AsyncValidator.ValidateObject(objectToBeValidated, validationContext, false);
        }

        // ValidateObject_throws_ValidationException_if_validateAllProperties_is_true_and_Required_test_fails
        [Fact]
        public static async Task TestValidateObjectThrowsIfRequiredTestFails()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = null };
            var validationContext = new ValidationContext(objectToBeValidated);
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            // cannot check error message - not defined on ret builds
            Assert.Null(exception.Value);
        }

        [Fact]
        public static async Task ValidateObject_succeeds_if_validateAllProperties_is_true_and_all_attributes_are_valid()
        {
            var objectToBeValidated = new ToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            await AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true);
        }

        [Fact]
        public static async Task ValidateObject_throws_ValidationException_if_all_properties_are_valid_but_class_is_invalid()
        {
            var objectToBeValidated = new InvalidToBeValidated() { PropertyWithRequiredAttribute = "Valid Value" };
            var validationContext = new ValidationContext(objectToBeValidated);
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.IsType<ValidClassAttribute>(exception.ValidationAttribute);
            Assert.Equal(
                "ValidClassAttribute.IsValid failed for class of type " + typeof(InvalidToBeValidated).FullName,
                exception.ValidationResult.ErrorMessage);
            Assert.Equal(objectToBeValidated, exception.Value);
        }

        [Fact]
        public async Task ValidateObject_IValidatableObject_Success()
        {
            var instance = new ValidatableSuccess();
            var context = new ValidationContext(instance);

            await AsyncValidator.ValidateObject(instance, context);
        }

        [Fact]
        public async Task ValidateObject_IValidatableObject_Error()
        {
            var instance = new ValidatableError();
            var context = new ValidationContext(instance);
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(instance, context));
            Assert.Equal("error", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public async Task ValidateObject_IValidatableObject_Null()
        {
            var instance = new ValidatableNull();
            var context = new ValidationContext(instance);

            await AsyncValidator.ValidateObject(instance, context);
        }

        [Fact]
        public static async Task ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field is required.", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value",
                SecondPropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_type_attribute_fails_validation()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Valid Value",
                SecondPropertyToBeTested = "TypeInvalid"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field mustn't be \"TypeInvalid\".", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task ValidateObject_returns_false_if_all_properties_are_valid_but_metadatatype_class_has_unmatched_property_name()
        {
            var objectToBeValidated = new HasMetadataTypeWithUnmatchedProperties()
            {
                PropertyToBeTested = "Valid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithUnmatchedProperties), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithUnmatchedProperties));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The associated metadata type for type 'CoreDX.Extensions.Validation.Tests.AsyncValidatorTests+HasMetadataTypeWithUnmatchedProperties' contains the following unknown properties or fields: SecondPropertyToBeTested. Please make sure that the names of these members match the names of the properties on the main type.",
                exception.Message);
        }

        [Fact]
        public static async Task ValidateObject_returns_false_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var objectToBeValidated = new HasMetadataTypeToBeValidated()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));

            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value",
                exception.Message);
        }

        [Fact]
        public static async Task ValidateObject_returns_false_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var objectToBeValidated = new HasMetadataTypeWithComplementaryRequirements()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));

            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value",
                exception.Message);

            objectToBeValidated.PropertyToBeTested = null;
            objectToBeValidated.SecondPropertyToBeTested = "Not Phone #";

            exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.",
                exception.Message);

            objectToBeValidated.SecondPropertyToBeTested = "0800123456789";

            exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.",
                exception.Message);
        }

        [Fact]
        public static async Task ValidateObject_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var objectToBeValidated = new SelfMetadataType()
            {
                PropertyToBeTested = "Invalid Value"
            };
            var validationContext = new ValidationContext(objectToBeValidated);
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));

            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value",
                exception.Message);

            objectToBeValidated.PropertyToBeTested = null;
            objectToBeValidated.SecondPropertyToBeTested = "Not Phone #";

            exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateObject(objectToBeValidated, validationContext, true));
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.",
                exception.Message);
        }

        #endregion ValidateObject

        #region TryValidateProperty

        [Fact]
        public static async Task TryValidatePropertyThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateProperty(new object(), validationContext: null, validationResults: null));
        }

        [Fact]
        public static async Task TryValidatePropertyThrowsIf_value_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateProperty(null, s_estValidationContext, validationResults: null));
        }

        // TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_null_or_empty()
        [Fact]
        public static async Task TestTryValidatePropertyThrowsIfNullOrEmptyValidationContextMemberName()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = string.Empty;
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateProperty(null, validationContext, null));
        }

        [Fact]
        public static async Task TryValidatePropertyThrowsIf_ValidationContext_MemberName_does_not_exist_on_object()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NonExist";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.TryValidateProperty(null, validationContext, null));
        }

        [Fact]
        public static async Task TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_not_public()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "InternalProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = "ProtectedProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.TryValidateProperty(null, validationContext, null));

            validationContext.MemberName = "PrivateProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.TryValidateProperty(null, validationContext, null));
        }

        [Fact]
        public static async Task TryValidatePropertyThrowsIf_ValidationContext_MemberName_is_for_a_public_indexer()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "Item";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.TryValidateProperty(null, validationContext, validationResults: null));
        }

        [Fact]
        public static async Task TryValidatePropertyThrowsIf_value_passed_is_of_wrong_type_to_be_assigned_to_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            validationContext.MemberName = "NoAttributesProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value", () => AsyncValidator.TryValidateProperty(123, validationContext, validationResults: null));
        }

        [Fact]
        public static async Task TryValidatePropertyThrowsIf_null_passed_to_non_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            // cannot assign null to a non-value-type property
            validationContext.MemberName = "EnumProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value", () => AsyncValidator.TryValidateProperty(null, validationContext, validationResults: null));

            // cannot assign null to a non-nullable property
            validationContext.MemberName = "NonNullableProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value", () => AsyncValidator.TryValidateProperty(null, validationContext, validationResults: null));
        }

        [Fact]
        public static async Task TryValidateProperty_returns_true_if_null_passed_to_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NullableProperty";
            Assert.True(await AsyncValidator.TryValidateProperty(null, validationContext, validationResults: null));
        }

        [Fact]
        public static async Task TryValidateProperty_returns_true_if_no_attributes_to_validate()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            Assert.True(
                await AsyncValidator.TryValidateProperty("Any Value", validationContext, validationResults: null));
        }

        [Fact]
        public static async Task TryValidateProperty_returns_false_if_errors()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            Assert.False(
                await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateProperty_returns_false_if_Required_attribute_test_fails()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            Assert.False(
                await AsyncValidator.TryValidateProperty(null, validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(
                await AsyncValidator.TryValidateProperty(null, validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            // cannot check error message - not defined on ret builds
        }

        [Fact]
        public static async Task TryValidateProperty_collection_can_have_multiple_results()
        {
            ValidationContext validationContext = new ValidationContext(new HasDoubleFailureProperty());
            validationContext.MemberName = nameof(HasDoubleFailureProperty.WillAlwaysFailTwice);
            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateProperty("Nope", validationContext, results));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public static async Task TryValidateProperty_returns_true_if_all_attributes_are_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            Assert.True(
                await AsyncValidator.TryValidateProperty("Valid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.True(
                await AsyncValidator.TryValidateProperty("Valid Value", validationContext, validationResults));
            Assert.Equal(0, validationResults.Count);
        }

        [Fact]
        public static async Task TryValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            Assert.False(await AsyncValidator.TryValidateProperty(null, validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateProperty(null, validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The SecondPropertyToBeTested field is required.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateProperty_returns_true_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateProperty_returns_true_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeWithComplementaryRequirements());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
            Assert.Contains(validationResults, x => x.ErrorMessage == "The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.");
        }

        [Fact]
        public static async Task TryValidateProperty_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var validationContext = new ValidationContext(new SelfMetadataType());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, null));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            Assert.Equal(1, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value");

            validationContext.MemberName = "SecondPropertyToBeTested";
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, null));

            validationResults.Clear();
            Assert.False(await AsyncValidator.TryValidateProperty("Invalid Value", validationContext, validationResults));
            //Assert.Equal(1, validationResults.Count);
            Assert.Contains(validationResults, x => x.ErrorMessage == "The SecondPropertyToBeTested field is not a valid phone number.");
        }

        #endregion TryValidateProperty

        #region ValidateProperty

        [Fact]
        public static async Task ValidatePropertyThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateProperty(new object(), validationContext: null));
        }

        [Fact]
        public static async Task ValidatePropertyThrowsIf_value_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateProperty(null, s_estValidationContext));
        }

        [Fact]
        public static async Task ValidatePropertyThrowsIf_ValidationContext_MemberName_is_null_or_empty()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateProperty(null, validationContext));

            validationContext.MemberName = string.Empty;
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static async Task ValidatePropertyThrowsIf_ValidationContext_MemberName_does_not_exist_on_object()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NonExist";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static async Task ValidatePropertyThrowsIf_ValidationContext_MemberName_is_not_public()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "InternalProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.ValidateProperty(null, validationContext));

            validationContext.MemberName = "ProtectedProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.ValidateProperty(null, validationContext));

            validationContext.MemberName = "PrivateProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static async Task ValidatePropertyThrowsIf_ValidationContext_MemberName_is_for_a_public_indexer()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "Item";
            await AssertExtensions.ThrowsAsync<ArgumentException>("propertyName", () => AsyncValidator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static async Task ValidatePropertyThrowsIf_value_passed_is_of_wrong_type_to_be_assigned_to_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            validationContext.MemberName = "NoAttributesProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value", () => AsyncValidator.ValidateProperty(123, validationContext));
        }

        [Fact]
        public static async Task ValidatePropertyThrowsIf_null_passed_to_non_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());

            // cannot assign null to a non-value-type property
            validationContext.MemberName = "EnumProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value", () => AsyncValidator.ValidateProperty(null, validationContext));

            // cannot assign null to a non-nullable property
            validationContext.MemberName = "NonNullableProperty";
            await AssertExtensions.ThrowsAsync<ArgumentException>("value", () => AsyncValidator.ValidateProperty(null, validationContext));
        }

        [Fact]
        public static async Task ValidateProperty_succeeds_if_null_passed_to_nullable_property()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NullableProperty";
            await AsyncValidator.ValidateProperty(null, validationContext);
        }

        [Fact]
        public static async Task ValidateProperty_succeeds_if_no_attributes_to_validate()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            await AsyncValidator.ValidateProperty("Any Value", validationContext);
        }

        [Fact]
        public static async Task ValidateProperty_throws_ValidationException_if_errors()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static async Task ValidateProperty_throws_ValidationException_if_Required_attribute_test_fails()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty(null, validationContext));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            // cannot check error message - not defined on ret builds
            Assert.Null(exception.Value);
        }

        [Fact]
        public static async Task ValidateProperty_succeeds_if_all_attributes_are_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            await AsyncValidator.ValidateProperty("Valid Value", validationContext);
        }

        [Fact]
        public static async Task ValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_required_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty(null, validationContext));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            Assert.Null(exception.Value);
        }

        [Fact]
        public static async Task ValidateProperty_returns_false_if_all_properties_are_valid_but_metadatatype_class_property_attribute_fails_validation()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "SecondPropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<MaxLengthAttribute>(exception.ValidationAttribute);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", exception.ValidationResult.ErrorMessage);
        }

        [Fact]
        public static async Task ValidateProperty_returns_false_if_property_attribute_is_not_removed_by_metadatatype_class()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeToBeValidated), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeToBeValidated));
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static async Task ValidateProperty_returns_false_if_property_has_attributes_from_base_and_metadatatype_classes()
        {
            var validationContext = new ValidationContext(new HasMetadataTypeWithComplementaryRequirements());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(HasMetadataTypeWithComplementaryRequirements), typeof(MetadataTypeToAddValidationAttributes)), typeof(HasMetadataTypeWithComplementaryRequirements));
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);

            validationContext.MemberName = "SecondPropertyToBeTested";
            exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("Not Phone #", validationContext));
            Assert.IsType<PhoneAttribute>(exception.ValidationAttribute);
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Not Phone #", exception.Value);

            exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("0800123456789", validationContext));
            Assert.IsType<MaxLengthAttribute>(exception.ValidationAttribute);
            Assert.Equal("The field SecondPropertyToBeTested must be a string or array type with a maximum length of '11'.", exception.ValidationResult.ErrorMessage);
            Assert.Equal("0800123456789", exception.Value);
        }

        [Fact]
        public static async Task ValidateProperty_returns_false_if_validation_fails_when_class_references_itself_as_a_metadatatype()
        {
            var validationContext = new ValidationContext(new SelfMetadataType());
            validationContext.MemberName = "PropertyToBeTested";
            TypeDescriptor.AddProviderTransparent(new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SelfMetadataType), typeof(SelfMetadataType)), typeof(SelfMetadataType));
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);

            validationContext.MemberName = "SecondPropertyToBeTested";
            exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateProperty("Invalid Value", validationContext));
            Assert.IsType<PhoneAttribute>(exception.ValidationAttribute);
            Assert.Equal("The SecondPropertyToBeTested field is not a valid phone number.", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        #endregion ValidateProperty

        #region TryValidateValue

        [Fact]
        public static async Task TryValidateValueThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateValue(new object(),
                    validationContext: null, validationResults: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static async Task TryValidateValueThrowsIf_ValidationAttributeEnumerable_is_null()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.TryValidateValue(new object(), validationContext, validationResults: null, validationAttributes: null));
        }

        [Fact]
        public static async Task TryValidateValue_returns_true_if_no_attributes_to_validate_regardless_of_value()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            Assert.True(await AsyncValidator.TryValidateValue(null, validationContext,
                validationResults: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
            Assert.True(await AsyncValidator.TryValidateValue(new object(), validationContext,
                validationResults: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static async Task TryValidateValue_returns_false_if_Property_has_RequiredAttribute_and_value_is_null()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.False(await AsyncValidator.TryValidateValue(null, validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateValue(null, validationContext, validationResults, attributesToValidate));
            Assert.Equal(1, validationResults.Count);
            // cannot check error message - not defined on ret builds
        }

        [Fact]
        public static async Task TryValidateValue_returns_false_if_Property_has_RequiredAttribute_and_value_is_invalid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.False(await AsyncValidator.TryValidateValue("Invalid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateValue("Invalid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateValue_collection_can_have_multiple_results()
        {
            ValidationContext validationContext = new ValidationContext(new HasDoubleFailureProperty());
            validationContext.MemberName = nameof(HasDoubleFailureProperty.WillAlwaysFailTwice);
            ValidationAttribute[] attributesToValidate =
                {new ValidValueStringPropertyAttribute(), new ValidValueStringPropertyDuplicateAttribute()};

            List<ValidationResult> results = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateValue("Not Valid", validationContext, results, attributesToValidate));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public static async Task TryValidateValue_returns_true_if_Property_has_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            Assert.True(await AsyncValidator.TryValidateValue("Valid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.True(await AsyncValidator.TryValidateValue("Valid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(0, validationResults.Count);
        }

        [Fact]
        public static async Task TryValidateValue_returns_false_if_Property_has_no_RequiredAttribute_and_value_is_invalid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            Assert.False(await AsyncValidator.TryValidateValue("Invalid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.False(await AsyncValidator.TryValidateValue("Invalid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(1, validationResults.Count);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", validationResults[0].ErrorMessage);
        }

        [Fact]
        public static async Task TryValidateValue_returns_true_if_Property_has_no_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            Assert.True(await AsyncValidator.TryValidateValue("Valid Value", validationContext, null, attributesToValidate));

            var validationResults = new List<ValidationResult>();
            Assert.True(await AsyncValidator.TryValidateValue("Valid Value", validationContext, validationResults, attributesToValidate));
            Assert.Equal(0, validationResults.Count);
        }

        #endregion TryValidateValue

        #region ValidateValue

        [Fact]
        public static async Task ValidateValueThrowsIf_ValidationContext_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateValue(new object(),
                    validationContext: null, validationAttributes: Enumerable.Empty<ValidationAttribute>()));
        }

        [Fact]
        public static async Task ValidateValueThrowsIf_ValidationAttributeEnumerable_is_null()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = null;
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AsyncValidator.ValidateValue(new object(), validationContext, validationAttributes: null));
        }

        [Fact]
        public static async Task ValidateValue_succeeds_if_no_attributes_to_validate_regardless_of_value()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "NoAttributesProperty";
            await AsyncValidator.ValidateValue(null, validationContext, Enumerable.Empty<ValidationAttribute>());
            await AsyncValidator.ValidateValue(new object(), validationContext, Enumerable.Empty<ValidationAttribute>());
        }

        // ValidateValue_throws_ValidationException_if_Property_has_RequiredAttribute_and_value_is_null()
        [Fact]
        public static async Task TestValidateValueThrowsIfNullRequiredAttribute()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateValue(null, validationContext, attributesToValidate));
            Assert.IsType<RequiredAttribute>(exception.ValidationAttribute);
            // cannot check error message - not defined on ret builds
            Assert.Null(exception.Value);
        }

        // ValidateValue_throws_ValidationException_if_Property_has_RequiredAttribute_and_value_is_invalid()
        [Fact]
        public static async Task TestValidateValueThrowsIfRequiredAttributeInvalid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateValue("Invalid Value", validationContext, attributesToValidate));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static async Task ValidateValue_succeeds_if_Property_has_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new RequiredAttribute(), new ValidValueStringPropertyAttribute() };
            await AsyncValidator.ValidateValue("Valid Value", validationContext, attributesToValidate);
        }

        // ValidateValue_throws_ValidationException_if_Property_has_no_RequiredAttribute_and_value_is_invalid()
        [Fact]
        public static async Task TestValidateValueThrowsIfNoRequiredAttribute()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyWithRequiredAttribute";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => AsyncValidator.ValidateValue("Invalid Value", validationContext, attributesToValidate));
            Assert.IsType<ValidValueStringPropertyAttribute>(exception.ValidationAttribute);
            Assert.Equal("ValidValueStringPropertyAttribute.IsValid failed for value Invalid Value", exception.ValidationResult.ErrorMessage);
            Assert.Equal("Invalid Value", exception.Value);
        }

        [Fact]
        public static async Task ValidateValue_succeeds_if_Property_has_no_RequiredAttribute_and_value_is_valid()
        {
            var validationContext = new ValidationContext(new ToBeValidated());
            validationContext.MemberName = "PropertyToBeTested";
            var attributesToValidate = new ValidationAttribute[] { new ValidValueStringPropertyAttribute() };
            await AsyncValidator.ValidateValue("Valid Value", validationContext, attributesToValidate);
        }

        #endregion ValidateValue

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class ValidValueStringPropertyAttribute : AsyncValidationAttribute
        {
            protected override ValueTask<ValidationResult> IsValidAsync(object value, ValidationContext _, CancellationToken cancellationToken)
            {
                if (value == null) { return ValueTask.FromResult(ValidationResult.Success); }
                var valueAsString = value as string;
                if ("Valid Value".Equals(valueAsString)) { return ValueTask.FromResult(ValidationResult.Success); }
                return ValueTask.FromResult(new ValidationResult("ValidValueStringPropertyAttribute.IsValid failed for value " + value));
            }
        }

        // Allows easy testing that multiple failures can be reported
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class ValidValueStringPropertyDuplicateAttribute : AsyncValidationAttribute
        {
            protected override ValueTask<ValidationResult> IsValidAsync(object value, ValidationContext _, CancellationToken cancellationToken)
            {
                if (value == null)
                { return ValueTask.FromResult(ValidationResult.Success); }
                var valueAsString = value as string;
                if ("Valid Value".Equals(valueAsString))
                { return ValueTask.FromResult(ValidationResult.Success); }
                return ValueTask.FromResult(new ValidationResult("ValidValueStringPropertyAttribute.IsValid failed for value " + value));
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class ValidClassAttribute : AsyncValidationAttribute
        {
            protected override ValueTask<ValidationResult> IsValidAsync(object value, ValidationContext _, CancellationToken cancellationToken)
            {
                if (value == null)
                { return ValueTask.FromResult(ValidationResult.Success); }
                if (value.GetType().Name.ToLowerInvariant().Contains("invalid"))
                {
                    return ValueTask.FromResult(new ValidationResult("ValidClassAttribute.IsValid failed for class of type " + value.GetType().FullName));
                }
                return ValueTask.FromResult(ValidationResult.Success);
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class ValidClassDuplicateAttribute : AsyncValidationAttribute
        {
            protected override ValueTask<ValidationResult> IsValidAsync(object value, ValidationContext _, CancellationToken cancellationToken)
            {
                if (value == null)
                { return ValueTask.FromResult(ValidationResult.Success); }
                if (value.GetType().Name.ToLowerInvariant().Contains("invalid"))
                {
                    return ValueTask.FromResult(new ValidationResult("ValidClassAttribute.IsValid failed for class of type " + value.GetType().FullName));
                }
                return ValueTask.FromResult(ValidationResult.Success);
            }
        }

        public class HasDoubleFailureProperty
        {
            [ValidValueStringProperty, ValidValueStringPropertyDuplicate]
            public string WillAlwaysFailTwice => "This is never valid.";
        }

        [ValidClass, ValidClassDuplicate]
        public class DoublyInvalid
        {
        }

        [ValidClass]
        public class ToBeValidated
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string NoAttributesProperty { get; set; }

            [Required]
            [ValidValueStringProperty]
            public string PropertyWithRequiredAttribute { get; set; }

            internal string InternalProperty { get; set; }
            protected string ProtectedProperty { get; set; }
            private string PrivateProperty { get; set; }

            public string this[int index]
            {
                get { return null; }
                set { }
            }

            public TestEnum EnumProperty { get; set; }

            public int NonNullableProperty { get; set; }
            public int? NullableProperty { get; set; }

            // Private properties should not be validated.

            [Required]
            private string PrivateSetOnlyProperty { set { } }

            [Required]
            protected string ProtectedSetOnlyProperty { set { } }

            [Required]
            internal string InternalSetOnlyProperty { set { } }

            [Required]
            protected internal string ProtectedInternalSetOnlyProperty { set { } }

            [Required]
            private string PrivateGetOnlyProperty { get; }

            [Required]
            protected string ProtectedGetOnlyProperty { get; }

            [Required]
            internal string InternalGetOnlyProperty { get; }

            [Required]
            protected internal string ProtectedInternalGetOnlyProperty { get; }
        }

        public enum TestEnum
        {
            A = 0
        }

        [ValidClass]
        public class InvalidToBeValidated
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string NoAttributesProperty { get; set; }

            [Required]
            [ValidValueStringProperty]
            public string PropertyWithRequiredAttribute { get; set; }
        }

        public class HasMetadataTypeToBeValidated
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string SecondPropertyToBeTested { get; set; }
        }

        public class HasMetadataTypeWithUnmatchedProperties
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            public string MismatchedNameProperty { get; set; }
        }

        public class HasMetadataTypeWithComplementaryRequirements
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            [Phone]
            public string SecondPropertyToBeTested { get; set; }
        }

        public class SelfMetadataType
        {
            [ValidValueStringProperty]
            public string PropertyToBeTested { get; set; }

            [Phone]
            public string SecondPropertyToBeTested { get; set; }
        }

        [CustomValidation(typeof(MetadataTypeToAddValidationAttributes), nameof(Validate))]
        public class MetadataTypeToAddValidationAttributes
        {
            [Required]
            [MaxLength(11)]
            public string SecondPropertyToBeTested { get; set; }

            public static ValidationResult Validate(HasMetadataTypeToBeValidated value)
                => value.SecondPropertyToBeTested == "TypeInvalid"
                    ? new ValidationResult("The SecondPropertyToBeTested field mustn't be \"TypeInvalid\".")
                    : ValidationResult.Success;
        }
    }
}