# TransactionScope и savepoints

Если у родительского скоупа свойство `SavepointExecutor` не равно `null`, то вложенный скоуп создает точку сохранения. Если вложенный скоуп завершается без `Complete`, то происходит откат до точки сохранения.
```csharp
using (new LocalTransactionScope { SavepointExecutor = savepointExecutor })
{
    ...
    using (new LocalTransactionScope()) //создается точка сохранения SP1
    {
        ...    
    }//откат до точки сохранения SP1
    ...
}
```

# Сценарии использования

## Сценарий 1
По [ссылке](SavepointHandlers.SqlServer.Tests/Tests.cs) пример использования TransactionScope в сочетании с [Class Fixtures](https://xunit.net/docs/shared-context#class-fixture) из xUnit.

## Сценарий 2
Использование TransactionScope внутри теста:
```csharp
//в конструкторе теста создается LocalTransactionScope и проставляется SavepointExecutor

[Fact]
public void SomeTest()
{
    ...
    TestCase1();
    ...
    TestCase2();
    ...
}

private void TestCase1()
{
    using var _ = new LocalTransactionScope();

    ...
}//откатываются все изменения в БД, сделанные в методе TestCase1


private void TestCase2()
{
    using var _ = new LocalTransactionScope();

    ...
}//откатываются все изменения в БД, сделанные в методе TestCase2
```

## Сценарий 3
Тестирование сценариев, в которых скоуп завершается без `Complete`:
```csharp
//в конструкторе теста создается LocalTransactionScope и проставляется SavepointExecutor

[Fact]
public void SomeTest()
{
    ...
    service.ApplicationMethod();
    ...
    //можно работать с БД и вызывать другие методы приложения
    ...
}

public class ApplicationService
{
    public void ApplicationMethod()
    {
        ...
        using (var scope = new LocalTransactionScope()) //создается точка сохранения SP1
        {
            ...            
            throw new Exception();
        }//откат до точки сохранения SP1
    }
}
```
