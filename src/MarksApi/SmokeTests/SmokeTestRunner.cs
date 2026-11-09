using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MarksApi.Options;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace MarksApi.SmokeTests;

public static class SmokeTestRunner
{
    private sealed record CheckResult(string Method, string Path, int Expected, int Actual)
    {
        public bool Passed => Expected == Actual;
    }

    public static async Task<int> RunAsync(WebApplication app, IConfiguration configuration)
    {
        await app.StartAsync();

        try
        {
            var server = app.Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses
                ?? throw new InvalidOperationException("Could not determine server address for smoke tests.");
            var baseUrl = addresses.First().TrimEnd('/');

            using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var results = new List<CheckResult>();
            var demo = configuration.GetSection(DemoUsersOptions.SectionName).Get<DemoUsersOptions>()
                ?? new DemoUsersOptions();

            if (string.IsNullOrWhiteSpace(demo.TeacherPassword) || string.IsNullOrWhiteSpace(demo.StudentPassword))
            {
                Console.WriteLine("FAIL config DemoUsers passwords missing. Set DemoUsers:TeacherPassword and DemoUsers:StudentPassword via user-secrets.");
                return 1;
            }

            async Task Check(string method, string path, int expected, HttpRequestMessage request)
            {
                using var response = await client.SendAsync(request);
                results.Add(new CheckResult(method, path, expected, (int)response.StatusCode));
            }

            // Health
            await Check("GET", "/health", 200, new HttpRequestMessage(HttpMethod.Get, "/health"));

            // Auth failures
            await Check("POST", "/api/auth/login", 400, new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { username = "", password = "" })
            });
            await Check("POST", "/api/auth/login", 401, new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { username = demo.TeacherUsername, password = "wrong-password" })
            });

            // Teacher login
            using (var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                username = demo.TeacherUsername,
                password = demo.TeacherPassword
            }))
            {
                results.Add(new CheckResult("POST", "/api/auth/login", 200, (int)loginResponse.StatusCode));
                loginResponse.EnsureSuccessStatusCode();
                using var loginDoc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
                var teacherToken = loginDoc.RootElement.GetProperty("token").GetString()
                    ?? throw new InvalidOperationException("Teacher token missing.");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacherToken);
            }

            // Unauthorized without token on students list — temporary clear
            var savedAuth = client.DefaultRequestHeaders.Authorization;
            client.DefaultRequestHeaders.Authorization = null;
            await Check("GET", "/api/students", 401, new HttpRequestMessage(HttpMethod.Get, "/api/students"));
            client.DefaultRequestHeaders.Authorization = savedAuth;

            // Positive teacher flows with SMOKE- prefix cleanup
            int studentId;
            using (var createStudent = await client.PostAsJsonAsync("/api/students", new
            {
                registrationNo = "SMOKE-REG-001",
                fullName = "SMOKE Student",
                email = "smoke.student@example.com"
            }))
            {
                results.Add(new CheckResult("POST", "/api/students", 201, (int)createStudent.StatusCode));
                createStudent.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await createStudent.Content.ReadAsStringAsync());
                studentId = doc.RootElement.GetProperty("id").GetInt32();
            }

            await Check("GET", "/api/students", 200, new HttpRequestMessage(HttpMethod.Get, "/api/students"));
            await Check("GET", $"/api/students/{studentId}", 200, new HttpRequestMessage(HttpMethod.Get, $"/api/students/{studentId}"));
            await Check("PUT", $"/api/students/{studentId}", 200, new HttpRequestMessage(HttpMethod.Put, $"/api/students/{studentId}")
            {
                Content = JsonContent.Create(new
                {
                    registrationNo = "SMOKE-REG-001",
                    fullName = "SMOKE Student Updated",
                    email = "smoke.student@example.com"
                })
            });

            // Duplicate student -> 409
            await Check("POST", "/api/students", 409, new HttpRequestMessage(HttpMethod.Post, "/api/students")
            {
                Content = JsonContent.Create(new
                {
                    registrationNo = "SMOKE-REG-001",
                    fullName = "Other",
                    email = "smoke.other@example.com"
                })
            });

            // Bad body -> 400
            await Check("POST", "/api/students", 400, new HttpRequestMessage(HttpMethod.Post, "/api/students")
            {
                Content = JsonContent.Create(new { registrationNo = "SMOKE-X", fullName = "", email = "bad" })
            });

            int courseId;
            using (var createCourse = await client.PostAsJsonAsync("/api/courses", new
            {
                code = "SMK1",
                title = "SMOKE Course",
                creditHours = 3
            }))
            {
                results.Add(new CheckResult("POST", "/api/courses", 201, (int)createCourse.StatusCode));
                createCourse.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await createCourse.Content.ReadAsStringAsync());
                courseId = doc.RootElement.GetProperty("id").GetInt32();
            }

            await Check("GET", "/api/courses", 200, new HttpRequestMessage(HttpMethod.Get, "/api/courses"));
            await Check("GET", $"/api/courses/{courseId}", 200, new HttpRequestMessage(HttpMethod.Get, $"/api/courses/{courseId}"));
            await Check("PUT", $"/api/courses/{courseId}", 200, new HttpRequestMessage(HttpMethod.Put, $"/api/courses/{courseId}")
            {
                Content = JsonContent.Create(new { code = "SMK1", title = "SMOKE Course Updated", creditHours = 2 })
            });
            await Check("POST", "/api/courses", 409, new HttpRequestMessage(HttpMethod.Post, "/api/courses")
            {
                Content = JsonContent.Create(new { code = "SMK1", title = "Dup", creditHours = 1 })
            });
            await Check("GET", "/api/courses/999999", 404, new HttpRequestMessage(HttpMethod.Get, "/api/courses/999999"));

            int enrollmentId;
            using (var enroll = await client.PostAsJsonAsync($"/api/courses/{courseId}/enrollments", new { studentId }))
            {
                results.Add(new CheckResult("POST", $"/api/courses/{courseId}/enrollments", 201, (int)enroll.StatusCode));
                enroll.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await enroll.Content.ReadAsStringAsync());
                enrollmentId = doc.RootElement.GetProperty("id").GetInt32();
            }

            await Check("POST", $"/api/courses/{courseId}/enrollments", 409, new HttpRequestMessage(HttpMethod.Post, $"/api/courses/{courseId}/enrollments")
            {
                Content = JsonContent.Create(new { studentId })
            });
            await Check("GET", $"/api/courses/{courseId}/enrollments", 200, new HttpRequestMessage(HttpMethod.Get, $"/api/courses/{courseId}/enrollments"));
            await Check("PUT", $"/api/enrollments/{enrollmentId}/mark", 200, new HttpRequestMessage(HttpMethod.Put, $"/api/enrollments/{enrollmentId}/mark")
            {
                Content = JsonContent.Create(new { mark = 85 })
            });
            await Check("PUT", $"/api/enrollments/{enrollmentId}/mark", 400, new HttpRequestMessage(HttpMethod.Put, $"/api/enrollments/{enrollmentId}/mark")
            {
                Content = JsonContent.Create(new { mark = 101 })
            });
            await Check("PUT", $"/api/enrollments/{enrollmentId}/mark", 400, new HttpRequestMessage(HttpMethod.Put, $"/api/enrollments/{enrollmentId}/mark")
            {
                Content = JsonContent.Create(new { mark = -1 })
            });
            await Check("GET", $"/api/courses/{courseId}/stats", 200, new HttpRequestMessage(HttpMethod.Get, $"/api/courses/{courseId}/stats"));
            await Check("GET", $"/api/students/{studentId}/transcript", 200, new HttpRequestMessage(HttpMethod.Get, $"/api/students/{studentId}/transcript"));

            // Student auth + 403 cases
            string studentToken;
            using (var studentLogin = await client.PostAsJsonAsync("/api/auth/login", new
            {
                username = demo.StudentUsername,
                password = demo.StudentPassword
            }))
            {
                results.Add(new CheckResult("POST", "/api/auth/login (student)", 200, (int)studentLogin.StatusCode));
                studentLogin.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await studentLogin.Content.ReadAsStringAsync());
                studentToken = doc.RootElement.GetProperty("token").GetString()
                    ?? throw new InvalidOperationException("Student token missing.");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);
            await Check("GET", "/api/students", 403, new HttpRequestMessage(HttpMethod.Get, "/api/students"));
            await Check("GET", $"/api/students/{studentId}", 403, new HttpRequestMessage(HttpMethod.Get, $"/api/students/{studentId}"));
            await Check("GET", "/api/courses", 200, new HttpRequestMessage(HttpMethod.Get, "/api/courses"));

            // Cleanup with teacher token
            client.DefaultRequestHeaders.Authorization = savedAuth;
            await Check("DELETE", $"/api/enrollments/{enrollmentId}", 204, new HttpRequestMessage(HttpMethod.Delete, $"/api/enrollments/{enrollmentId}"));
            await Check("DELETE", $"/api/courses/{courseId}", 204, new HttpRequestMessage(HttpMethod.Delete, $"/api/courses/{courseId}"));
            await Check("DELETE", $"/api/students/{studentId}", 204, new HttpRequestMessage(HttpMethod.Delete, $"/api/students/{studentId}"));

            // Conflict delete student with enrollments (use seeded student 1 if present)
            await Check("DELETE", "/api/students/1", 409, new HttpRequestMessage(HttpMethod.Delete, "/api/students/1"));

            foreach (var result in results)
            {
                var status = result.Passed ? "PASS" : "FAIL";
                Console.WriteLine($"{status} {result.Method} {result.Path} expected={result.Expected} actual={result.Actual}");
            }

            var passed = results.Count(x => x.Passed);
            var failed = results.Count - passed;
            Console.WriteLine($"SUMMARY passed={passed} failed={failed} total={results.Count}");
            return failed == 0 ? 0 : 1;
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
