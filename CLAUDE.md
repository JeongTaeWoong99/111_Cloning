# CLAUDE.md

---

## 환경

| 항목             | 내용                  |
|------------------|-----------------------|
| Unity 버전       | 6000.3.10.f1 LTS      |
| IDE              | Rider 25.2.1          |
| 렌더 파이프라인  | 2D URP                |

---

## Skills 참조

필요한 작업 시 `/` 슬래시 커맨드로 호출하거나, 관련 작업 시 자동 로드된다.

| 슬래시 커맨드         | 경로                                              | 내용                |
|-----------------------|---------------------------------------------------|---------------------|
| `/commit-convention`  | [`.claude/skills/commit-convention/SKILL.md`](.claude/skills/commit-convention/SKILL.md) | Git 커밋 메시지 규칙 |

---

## 주요 규칙

- 커밋 시 반드시 `/commit-convention` 규칙을 따른다.
- 에셋 원본 폴더는 `.gitignore`에 등록하여 GitHub 업로드에서 제외한다.
- 코드는 한글 주석 사용.
