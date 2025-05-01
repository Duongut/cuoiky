using System;
using Xunit;

namespace BasicTests
{
    public class UnitTest1
    {
        [Fact]
        public void SimpleTest_ShouldPass()
        {
            // Arrange
            int a = 2;
            int b = 3;
            
            // Act
            int result = a + b;
            
            // Assert
            Assert.Equal(5, result);
        }
        
        [Fact]
        public void StringTest_ShouldPass()
        {
            // Arrange
            string str1 = "Hello";
            string str2 = "World";
            
            // Act
            string result = $"{str1} {str2}";
            
            // Assert
            Assert.Equal("Hello World", result);
        }
        
        [Fact]
        public void BooleanTest_ShouldPass()
        {
            // Arrange
            bool condition = true;
            
            // Act & Assert
            Assert.True(condition);
        }
        
        [Fact]
        public void ExceptionTest_ShouldPass()
        {
            // Arrange
            Action action = () => throw new InvalidOperationException("Test exception");
            
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(action);
            Assert.Equal("Test exception", exception.Message);
        }
        
        [Fact]
        public void CollectionTest_ShouldPass()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            
            // Act & Assert
            Assert.Contains(3, collection);
            Assert.DoesNotContain(6, collection);
            Assert.Equal(5, collection.Length);
        }
    }
}
