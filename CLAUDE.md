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
| `/clean-code`         | [`.claude/skills/clean-code-style/SKILL.md`](.claude/skills/clean-code-style/SKILL.md) | Unity/C# 클린 코드 스타일 규칙 |
| `/feature-design`     | [`.claude/skills/feature-design/SKILL.md`](.claude/skills/feature-design/SKILL.md) | OOP·SOLID·디자인 패턴 기반 기능 설계 |
| `/optimization`       | [`.claude/skills/optimization/SKILL.md`](.claude/skills/optimization/SKILL.md) | 성능 최적화 판단 및 적용 가이드 |

---

## 주요 규칙

- 커밋 시 반드시 `/commit-convention` 규칙을 따른다.
- 코드 작성 시 `/clean-code` 규칙을 따른다.
- 기능 구현 시 `/feature-design` 규칙을 따른다.
- 최적화 작업 시 `/optimization` 규칙을 따른다.
- 에셋 원본 폴더는 `.gitignore`에 등록하여 GitHub 업로드에서 제외한다.
- 참고 자료는 `.claude/references/` 폴더에 보관하며, git에서 제외된다.
- 코드는 한글 주석 사용.
