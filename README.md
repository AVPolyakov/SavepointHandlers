# Полный откат транзакции в тесте

Полный откат транзакции в каждом тесте напоминает известный анекдот про маляра Шлемиэля:

>Маляр Шлемиэль подрядился красить пунктирные осевые линии на дорогах. В первый день он получил банку краски, поставил её на дорогу, и к концу дня покрасил 300 метров осевой линии. «Отлично! — сказал прораб. — Быстро работаешь!» — и заплатил ему копейку.
>
>На следующий день Шлемиэль покрасил 150 метров. «Мда, это, конечно, не так здорово, как вчера, но приемлемо», — сказал прораб и заплатил ему копейку.
>
>На следующий день Шлемиэль покрасил 30 метров. «Всего лишь 30! — заорал прораб. — Это никуда не годится! В первый день было в десять раз больше! В чём дело?»
>
>«Ничего не могу поделать, — говорит Шлемиэль. — Каждый день я ухожу всё дальше и дальше от банки с краской!»

Точки сохранения позволяют перемещать "банку с краской" туда, откуда тестирование выполняется быстрее. В данном случае "банка с краской" — это состояние базы данных.

# TransactionScope и savepoints

Если у родительского скоупа параметр `savepointExecutor` не равен `null`, то вложенный скоуп создает точку сохранения. Если вложенный скоуп завершается без `Complete`, то происходит откат до точки сохранения.
```csharp
using (LocalTransactionScopeFactory.Create(savepointExecutor))
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
Тестирование сценариев приложения, в которых скоуп завершается без `Complete`:
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