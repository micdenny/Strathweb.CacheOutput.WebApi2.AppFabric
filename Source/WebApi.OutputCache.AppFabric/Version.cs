using System.Reflection;

// version number: <major>.<minor>.<non-breaking-feature>.<build>
[assembly: AssemblyVersion("0.2.1.0")]

// Note: until version 1.0 expect breaking changes on 0.X versions.

// 0.2.1.0 Using Put instead of Add to avoid exception on trying adding a value that already exist in cache, instead it inserts the new value and resets the expiration
// 0.2.0.0 Removed NotSupportedException
// 0.1.0.0 First commit