import json
from pathlib import Path

ROOT = Path(__file__).parent


def req(name, method, path, body=None, auth=None, tests=None, extrascript=None, query=None):
    url = {
        "raw": "{{baseUrl}}" + path,
        "host": ["{{baseUrl}}"],
        "path": [p for p in path.strip("/").split("/") if p],
    }
    if query:
        url["query"] = [{"key": k, "value": v} for k, v in query]
        url["raw"] = "{{baseUrl}}" + path + "?" + "&".join(f"{k}={v}" for k, v in query)

    item = {
        "name": name,
        "request": {
            "method": method,
            "header": [{"key": "Content-Type", "value": "application/json"}] if body is not None else [],
            "url": url,
        },
        "event": [],
        "response": [],
    }

    if auth == "noauth":
        item["request"]["auth"] = {"type": "noauth"}
    elif auth == "bearer":
        item["request"]["auth"] = {
            "type": "bearer",
            "bearer": [{"key": "token", "value": "{{token}}", "type": "string"}],
        }
    elif auth == "student":
        item["request"]["auth"] = {
            "type": "bearer",
            "bearer": [{"key": "token", "value": "{{studentToken}}", "type": "string"}],
        }

    if body is not None:
        item["request"]["body"] = {
            "mode": "raw",
            "raw": json.dumps(body, indent=2),
            "options": {"raw": {"language": "json"}},
        }

    script_lines = tests or []
    if extrascript:
        script_lines = script_lines + extrascript

    if script_lines:
        item["event"].append(
            {
                "listen": "test",
                "script": {
                    "type": "text/javascript",
                    "exec": script_lines,
                },
            }
        )
    return item


def status_tests(code, extra=None):
    lines = [
        f"pm.test('Status is {code}', function () {{ pm.response.to.have.status({code}); }});",
        # 204 has no body; 401 from the auth middleware may also be empty
        "pm.test('Response is acceptable', function () { const code = pm.response.code; const len = pm.response.text().length; if (code === 204 || code === 401) { pm.expect(true).to.be.true; } else { pm.expect(len).to.be.above(0); } });",
    ]
    if extra:
        lines.extend(extra)
    return lines


collection = {
    "info": {
        "name": "Marks API",
        "description": "Assignment 1 Postman collection for Student Marks Management API",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
    },
    "auth": {
        "type": "bearer",
        "bearer": [{"key": "token", "value": "{{token}}", "type": "string"}],
    },
    "variable": [
        {"key": "studentId", "value": "1"},
        {"key": "courseId", "value": "1"},
        {"key": "enrollmentId", "value": "1"},
        {"key": "studentToken", "value": ""},
    ],
    "item": [],
}

auth_folder = {
    "name": "Auth",
    "item": [
        req(
            "Login Teacher",
            "POST",
            "/api/auth/login",
            body={"username": "teacher1", "password": "Teacher@123"},
            auth="noauth",
            tests=status_tests(
                200,
                [
                    "const j = pm.response.json();",
                    "pm.test('Returns token', function () { pm.expect(j.token).to.be.a('string'); });",
                    "pm.environment.set('token', j.token);",
                ],
            ),
        ),
        req(
            "Login Student",
            "POST",
            "/api/auth/login",
            body={"username": "student1", "password": "Student@123"},
            auth="noauth",
            tests=status_tests(
                200,
                [
                    "const j = pm.response.json();",
                    "pm.test('Student role', function () { pm.expect(j.role).to.eql('Student'); });",
                    "pm.collectionVariables.set('studentToken', j.token);",
                ],
            ),
        ),
    ],
}

students_folder = {
    "name": "Students",
    "item": [
        req("List Students", "GET", "/api/students", query=[("page", "1"), ("pageSize", "10"), ("search", ""), ("sort", "fullName")], tests=status_tests(200, ["pm.test('Paged shape', function(){ const j=pm.response.json(); pm.expect(j).to.have.property('items'); pm.expect(j).to.have.property('totalCount'); });"])),
        req(
            "Create Student",
            "POST",
            "/api/students",
            body={
                "registrationNo": "PM-2026-001",
                "fullName": "Postman Student",
                "email": "postman.student@example.com",
            },
            tests=status_tests(
                201,
                [
                    "pm.test('Location header', function(){ pm.expect(pm.response.headers.get('Location')).to.include('/api/students/'); });",
                    "const j=pm.response.json(); pm.collectionVariables.set('studentId', String(j.id));",
                ],
            ),
        ),
        req("Get Student", "GET", "/api/students/{{studentId}}", tests=status_tests(200, ["pm.test('Has id', function(){ pm.expect(pm.response.json().id).to.be.a('number'); });"])),
        req(
            "Update Student",
            "PUT",
            "/api/students/{{studentId}}",
            body={
                "registrationNo": "PM-2026-001",
                "fullName": "Postman Student Updated",
                "email": "postman.student@example.com",
            },
            tests=status_tests(200, ["pm.test('Name updated', function(){ pm.expect(pm.response.json().fullName).to.include('Updated'); });"]),
        ),
        req("Get Transcript", "GET", "/api/students/{{studentId}}/transcript", tests=status_tests(200, ["pm.test('Has courses', function(){ pm.expect(pm.response.json()).to.have.property('courses'); });"])),
        req("Get Seeded Transcript", "GET", "/api/students/1/transcript", tests=status_tests(200, ["pm.test('Weighted average exists or null', function(){ const j=pm.response.json(); pm.expect(j).to.have.property('weightedAverage'); });"])),
    ],
}

courses_folder = {
    "name": "Courses",
    "item": [
        req("List Courses", "GET", "/api/courses", query=[("page", "1"), ("pageSize", "10")], tests=status_tests(200, ["pm.test('Paged', function(){ pm.expect(pm.response.json().items).to.be.an('array'); });"])),
        req(
            "Create Course",
            "POST",
            "/api/courses",
            body={"code": "PM101", "title": "Postman Course", "creditHours": 3},
            tests=status_tests(
                201,
                [
                    "pm.test('Location header', function(){ pm.expect(pm.response.headers.get('Location')).to.include('/api/courses/'); });",
                    "const j=pm.response.json(); pm.collectionVariables.set('courseId', String(j.id));",
                ],
            ),
        ),
        req("Get Course", "GET", "/api/courses/{{courseId}}", tests=status_tests(200, ["pm.test('Has code', function(){ pm.expect(pm.response.json().code).to.be.a('string'); });"])),
        req(
            "Update Course",
            "PUT",
            "/api/courses/{{courseId}}",
            body={"code": "PM101", "title": "Postman Course Updated", "creditHours": 2},
            tests=status_tests(200, ["pm.test('Title updated', function(){ pm.expect(pm.response.json().title).to.include('Updated'); });"]),
        ),
        req("Course Stats Seeded", "GET", "/api/courses/1/stats", tests=status_tests(200, ["pm.test('Stats fields', function(){ const j=pm.response.json(); pm.expect(j).to.have.property('average'); pm.expect(j).to.have.property('enrolled'); });"])),
    ],
}

enroll_folder = {
    "name": "Enrollments and Marks",
    "item": [
        req(
            "Enroll Student In Course",
            "POST",
            "/api/courses/{{courseId}}/enrollments",
            body={"studentId": "{{studentId}}"},
            tests=status_tests(
                201,
                [
                    "const j=pm.response.json();",
                    "pm.test('Enrollment id', function(){ pm.expect(j.id).to.be.a('number'); });",
                    "pm.collectionVariables.set('enrollmentId', String(j.id));",
                    # studentId in JSON must be number; Postman substitutes strings - use script event instead
                ],
            ),
        ),
        req("List Course Enrollments", "GET", "/api/courses/{{courseId}}/enrollments", tests=status_tests(200, ["pm.test('Items array', function(){ pm.expect(pm.response.json().items).to.be.an('array'); });"])),
        req(
            "Set Mark",
            "PUT",
            "/api/enrollments/{{enrollmentId}}/mark",
            body={"mark": 88},
            tests=status_tests(200, ["pm.test('Mark saved', function(){ pm.expect(pm.response.json().mark).to.eql(88); });"]),
        ),
        req("Course Stats After Mark", "GET", "/api/courses/{{courseId}}/stats", tests=status_tests(200, ["pm.test('Graded >= 1', function(){ pm.expect(pm.response.json().graded).to.be.at.least(1); });"])),
        req("Delete Enrollment", "DELETE", "/api/enrollments/{{enrollmentId}}", tests=status_tests(204)),
        req("Delete Course", "DELETE", "/api/courses/{{courseId}}", tests=status_tests(204)),
        req("Delete Student", "DELETE", "/api/students/{{studentId}}", tests=status_tests(204)),
    ],
}

# Fix enroll body to use numeric studentId via pre-request style: use a special raw body with no quotes around variable - in Postman {{studentId}} inside JSON number works if written without quotes in raw string
enroll_folder["item"][0]["request"]["body"]["raw"] = '{\n  "studentId": {{studentId}}\n}'

reports_folder = {
    "name": "Reports",
    "item": [
        req("Seeded Course Stats", "GET", "/api/courses/1/stats", tests=status_tests(200, ["pm.test('Has passed', function(){ pm.expect(pm.response.json()).to.have.property('passed'); });"])),
        req("Seeded Student Transcript", "GET", "/api/students/1/transcript", tests=status_tests(200, ["pm.test('Has registrationNo', function(){ pm.expect(pm.response.json().registrationNo).to.be.a('string'); });"])),
        req("Student Own Transcript", "GET", "/api/students/1/transcript", auth="student", tests=status_tests(200, ["pm.test('Own transcript ok', function(){ pm.expect(pm.response.json().studentId).to.eql(1); });"])),
    ],
}

health_folder = {
    "name": "Health",
    "item": [
        req("Health Check", "GET", "/health", auth="noauth", tests=status_tests(200, ["pm.test('Healthy text', function(){ pm.expect(pm.response.text()).to.include('Healthy'); });"])),
    ],
}

negative_folder = {
    "name": "Negative Tests",
    "item": [
        req("400 Login Empty", "POST", "/api/auth/login", body={"username": "", "password": ""}, auth="noauth", tests=status_tests(400, ["pm.test('Problem details', function(){ pm.expect(pm.response.json()).to.have.property('status'); });"])),
        req("401 Login Wrong Password", "POST", "/api/auth/login", body={"username": "teacher1", "password": "wrong"}, auth="noauth", tests=status_tests(401, ["pm.test('Unauthorized', function(){ pm.expect(pm.response.code).to.eql(401); });"])),
        req("401 Students No Token", "GET", "/api/students", auth="noauth", tests=status_tests(401, ["pm.test('No token blocked', function(){ pm.expect(pm.response.code).to.eql(401); });"])),
        req(
            "400 Create Student Empty Name",
            "POST",
            "/api/students",
            body={"registrationNo": "X", "fullName": "", "email": "x@example.com"},
            tests=status_tests(400, ["pm.test('Validation error', function(){ pm.expect(pm.response.json().status).to.eql(400); });"]),
        ),
        req(
            "400 Mark 101",
            "PUT",
            "/api/enrollments/1/mark",
            body={"mark": 101},
            tests=status_tests(400, ["pm.test('Mark too high', function(){ pm.expect(pm.response.code).to.eql(400); });"]),
        ),
        req(
            "400 Mark -1",
            "PUT",
            "/api/enrollments/1/mark",
            body={"mark": -1},
            tests=status_tests(400, ["pm.test('Mark too low', function(){ pm.expect(pm.response.code).to.eql(400); });"]),
        ),
        req("403 Student List Students", "GET", "/api/students", auth="student", tests=status_tests(403, ["pm.test('Forbidden', function(){ pm.expect(pm.response.code).to.eql(403); });"])),
        req("403 Student Other Transcript", "GET", "/api/students/2/transcript", auth="student", tests=status_tests(403, ["pm.test('Other student forbidden', function(){ pm.expect(pm.response.code).to.eql(403); });"])),
        req("404 Unknown Course", "GET", "/api/courses/999999", tests=status_tests(404, ["pm.test('Not found', function(){ pm.expect(pm.response.code).to.eql(404); });"])),
        req(
            "409 Duplicate Student",
            "POST",
            "/api/students",
            body={"registrationNo": "CS-2022-001", "fullName": "Dup", "email": "dup.unique@example.com"},
            tests=status_tests(409, ["pm.test('Conflict', function(){ pm.expect(pm.response.code).to.eql(409); });"]),
        ),
        req(
            "409 Double Enrollment",
            "POST",
            "/api/courses/1/enrollments",
            body={"studentId": 1},
            tests=status_tests(409, ["pm.test('Already enrolled', function(){ pm.expect(pm.response.code).to.eql(409); });"]),
        ),
        req("409 Delete Student With Enrollments", "DELETE", "/api/students/1", tests=status_tests(409, ["pm.test('Cannot delete', function(){ pm.expect(pm.response.code).to.eql(409); });"])),
    ],
}

collection["item"] = [
    auth_folder,
    students_folder,
    courses_folder,
    enroll_folder,
    reports_folder,
    health_folder,
    negative_folder,
]

(ROOT / "MarksApi.postman_collection.json").write_text(json.dumps(collection, indent=2), encoding="utf-8")

env = {
    "id": "local-marks-api",
    "name": "Local",
    "values": [
        {"key": "baseUrl", "value": "http://localhost:5041", "enabled": True},
        {"key": "token", "value": "", "enabled": True},
    ],
    "_postman_variable_scope": "environment",
}
(ROOT / "Local.postman_environment.json").write_text(json.dumps(env, indent=2), encoding="utf-8")

# count requests
count = 0
for folder in collection["item"]:
    count += len(folder["item"])
print("COLLECTION_OK requests=", count)
