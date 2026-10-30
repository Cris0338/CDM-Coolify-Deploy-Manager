// Copyright (c) 2026 Cris0338. Licensed under the MIT License.
using CoolifyDeployManager;
var s = new UserSettings();
var tests = new (string name, string branch, string project, string env, DeploymentGroup expected)[]
{
    ("example:release/service/api", "release/service/api", "EXAMPLE-SUITE", "api", DeploymentGroup.Production),
    ("example:staging/service/api-staging", "staging/service/api", "EXAMPLE-SUITE", "api", DeploymentGroup.Staging),
    ("admin-production", "main", "EXAMPLE-SUITE", "production", DeploymentGroup.Production),
    ("portal-staging", "test", "EXAMPLE-SUITE", "production", DeploymentGroup.Staging),
    ("portal", "release/portal", "EXAMPLE-SUITE", "production", DeploymentGroup.Production),
    ("portal-release-staging", "main", "EXAMPLE-SUITE", "api", DeploymentGroup.Unknown),
    ("chat", "main", "EXAMPLE-SUITE", "api", DeploymentGroup.Unknown),
    ("chat", "main", "EXAMPLE-SUITE", "staging", DeploymentGroup.Staging)
};
int failed = 0;
foreach (var (name, branch, project, env, expected) in tests)
{
    var app = new CoolifyApplication(Guid.NewGuid().ToString(), name, "running:healthy", branch, project, "p", env, "");
    var actual = Classifier.Classify(app, s);
    if (actual != expected) { Console.Error.WriteLine($"FAIL {name}: {actual} (expected {expected})"); failed++; }
    else Console.WriteLine($"PASS {name}: {actual}");
}
Console.WriteLine($"{tests.Length-failed}/{tests.Length} PASS");
return failed == 0 ? 0 : 1;
