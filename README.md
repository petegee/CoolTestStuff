# CoolTestStuff
## My Auto-mocking Container and a Fake builder

This is my crusty automocking container/base test class which:
* Automocks your class under test, saving you from ceremony cruft code and concentrate on the intent of the test.
* Supports partial mocking of the SUT. 
* Supports injection of supplied instances as required, else it will mock all dependencies automatically
* Lazy instantiation of the SUT.
* Has a Faker class that you can use to build Fake instances, which you can inject into your SUT for n-level deep System-Under-Test style tests.
* Works with nUnit only and provides template methods to hook into the test pipeline.
