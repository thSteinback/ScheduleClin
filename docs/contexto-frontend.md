# Contexto — Estilização do Front (ScheduleClin)

> Snapshot do estado atual do front-end, para servir de referência em mudanças futuras.
> Stack: **ASP.NET Core MVC (.NET 8)** com Razor Views (`.cshtml`). Não há SPA, build de CSS/JS nem framework de componentes — é HTML server-side + CSS/JS escritos à mão.

---

## ⚠️ Regras de trabalho (obrigatórias)

1. **Não editar nada sem aprovação prévia do plano de edição.** Antes de qualquer alteração, apresentar o plano e aguardar a aprovação explícita do responsável.
2. **Não alterar o que já foi feito.** O código/estilo existente deve ser preservado. Caso seja necessário mexer em algo já implementado, **sinalizar primeiro** e aguardar autorização — nunca alterar por conta própria.

---

## 1. Visão geral

O projeto tem **dois "mundos" visuais distintos e desconectados**:

| Mundo | Layout | Estilo | Telas |
|-------|--------|--------|-------|
| **Área pública / auth** | `_Layout.cshtml` (e telas sem layout) | Bootstrap 5 padrão (template default do ASP.NET) | Home, Login, Trocar senha, Acesso negado |
| **Área administrativa** | `_AdminLayout.cshtml` | Design system próprio (CSS custom, prefixo `adm-`) | Usuários, Agenda Geral, Consultas |

Essa divisão é a característica mais marcante hoje: a parte admin tem uma identidade visual cuidada e moderna, enquanto a parte pública/auth ainda usa o boilerplate cru do Bootstrap.

---

## 2. Dependências de estilo

- **Bootstrap 5** — `wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` (carregado em todos os layouts, mas praticamente só usado de fato nas telas públicas/auth).
- **Tabler Icons 3.33** — via CDN (`cdn.jsdelivr.net/.../tabler-icons-webfont`), carregado **apenas** no `_AdminLayout`. Usado com classes `ti ti-*` (ex.: `ti-users`, `ti-calendar`, `ti-plus`, `ti-pencil`).
- **jQuery + bootstrap.bundle.js** — carregados em ambos os layouts (jQuery praticamente não é usado; o JS é vanilla).
- **`wwwroot/css/site.css`** — overrides genéricos do template default (font-size responsivo, focus ring, footer fixo). Pouco relevante para a área admin.
- **`_Layout.cshtml.css`** — CSS isolado por view (scoped) do template default; só afeta `_Layout`.

> Observação: o `_AdminLayout` referencia `font-family: 'Inter'`, mas a fonte **não é importada** (nem Google Fonts nem `@font-face`). Na prática cai no fallback `system-ui, sans-serif`.

---

## 3. Design system da área admin (`_AdminLayout.cshtml`)

Todo o CSS do admin vive **inline, num único `<style>` dentro do `_AdminLayout.cshtml`** (≈230 linhas). Não há arquivo `.css` separado para isso. As classes seguem o prefixo `adm-`.

### Paleta de cores
- **Primária (azul):** `#2563eb` (hover `#1d4ed8`)
- **Fundo da página:** `#f4f6fb`
- **Superfícies/cards:** `#fff`
- **Bordas:** `#e8eaf0` (estrutura) e `#f3f4f6` (linhas internas de tabela)
- **Texto:** `#111827` (forte), `#374151` (corpo), `#6b7280` (secundário), `#9ca3af` (placeholder/muted)
- **Erro/perigo:** `#b91c1c` / `#ef4444`

### Tipografia
- Família: `'Inter', system-ui, sans-serif` (não carregada — ver nota acima)
- Títulos de página: `.adm-page-title` (22px/600), subtítulo `.adm-page-sub` (14px, muted)
- Labels de formulário: 11px, uppercase, `letter-spacing`

### Layout estrutural
- `.adm-shell` — flex, `height: 100vh`, overflow hidden
- `.adm-sidebar` — sidebar fixa de **240px**, branca, com logo, nav e rodapé de usuário/logout
- `.adm-nav-link` — itens de menu com estado `.active` (azul + borda esquerda)
- `.adm-main` / `.adm-content` — área de conteúdo com scroll próprio e padding `32px 36px`

### Componentes (todos com prefixo `adm-`)
- **Cards:** `.adm-card` (border-radius 12px)
- **Tabelas:** `.adm-table` — cabeçalho uppercase cinza, linhas com hover, `.row-inativo` reduz opacidade
- **Avatares:** `.av` / `.adm-avatar` — círculos com iniciais; cor gerada por hash do nome (paleta de 6 cores definida tanto no JS do layout quanto repetida em cada view)
- **Badges:** `.adm-badge` + variações de cor (`-blue`, `-teal`, `-purple`, `-gray`, `-red`, `-orange`, `-green`)
- **Botões:** `.adm-btn`, `.adm-btn-primary`, `.adm-btn-sm`, `.adm-icon-btn`
- **Toggle switch:** `.adm-toggle` (custom, estilo iOS)
- **Busca:** `.adm-search` / `.adm-search-wrap`
- **Formulários:** `.adm-field`, `.adm-input` (com focus ring azul)
- **Modais:** `.adm-modal-bg` (overlay) + `.adm-modal`; abrem/fecham via classe `.open` e helpers JS `openModal()/closeModal()`
- **Toast:** `.adm-toast` global, controlado por `showToast(msg, ok)`

### JS de apoio (inline no layout)
Funções globais: `openModal`, `closeModal`, `showToast`, fechar modal ao clicar fora, e `avatarColor()` (cor de avatar por iniciais via `data-av-name`).

---

## 4. Telas e como estão hoje

### Área admin (design system próprio)
- **`Admin/Users.cshtml` (Usuários):** tabela completa, modais de criar/editar, toggle de status, filtro por perfil e busca. Render dinâmico via `fetch` para `/get-users` e `/api/User`. Bem alinhada ao design system.
- **`Admin/Calendar.cshtml` (Agenda Geral):** calendário semanal **construído à mão** com CSS Grid. Tem seu **próprio `<style>` local** (prefixo `cal-`) — grade de horários 07h–19h, chips de consulta posicionados por `top/height` calculados em JS. Consome `/api/Calendar`.
- **`Admin/Queries.cshtml` (Consultas):** tabela de consultas com filtros (status, psicólogo) e busca. Usa `_ModalQueries`.
- **`Admin/_ModalQueries.cshtml`:** partial compartilhada (Agenda + Consultas) com os modais "Nova Consulta" e "Detalhes da Consulta" + todo o JS de CRUD de consultas. Mistura classes `adm-*` com bastante **estilo inline** (`style="..."`).

### Área pública / auth (Bootstrap cru)
- **`Account/Login.cshtml`:** `Layout = null`, Bootstrap puro, card centralizado simples (`max-width: 420px`). Sem identidade visual da clínica.
- **`Account/ChangePassword.cshtml`:** form Bootstrap dentro do `_Layout` default.
- **`Account/AccessDenied.cshtml`:** página 403 simples (Bootstrap).
- **`Home/Index.cshtml`:** página inicial básica (Bootstrap), lista perfis do usuário, botões "Trocar senha"/"Sair". Usa `_Layout` default com navbar "Schedule_V1", links Home/Privacy e footer "© 2026".
- **`Login/Index.cshtml`:** **arquivo vazio** (só `<body>` em branco) — provavelmente legado/morto; o login real é `Account/Login`.

---

## 5. Inconsistências e pontos de atenção

1. **Dois design systems desconexos:** admin (custom, polido) vs. público/auth (Bootstrap default). A experiência de login não combina com a do painel.
2. **Nomenclatura inconsistente da marca:** `_Layout` usa "Schedule_V1"; `_AdminLayout`/Login usam "ScheduleClin"; sidebar mostra "Clínica Bem-Estar".
3. **CSS espalhado:** o design admin está inline no `_AdminLayout`, e ainda há `<style>` locais (`Calendar`) e muito `style="..."` inline nas views/modais. Não há um `.css` central reutilizável.
4. **Fonte 'Inter' declarada mas não carregada.**
5. **Código duplicado em JS:** a paleta `COLORS` e a função `avColor`/`avatarColor` aparecem repetidas no layout e em várias views.
6. **`id="ed-id"` duplicado:** `_ModalQueries` e `Users.cshtml` usam o mesmo id `ed-id` — não convivem na mesma tela hoje, mas é um risco.
7. **Bootstrap + jQuery carregados mas pouco usados na área admin** (peso desnecessário).
8. **`Login/Index.cshtml` vazio** — candidato a remoção.
9. **Sem dark mode, sem variáveis CSS (`:root`/custom properties)** — cores hardcoded e repetidas em vários lugares, dificultando temização.
10. **Sem responsividade real no admin:** sidebar de 240px fixa e `height:100vh` não têm tratamento para mobile (o calendário em grid também não).

---

## 6. Resumo rápido para decisões futuras

- **O que está bom:** a identidade visual da área admin (clean, azul `#2563eb`, cards arredondados, Tabler Icons) é coerente e moderna. Vale usá-la como base do design system oficial.
- **Maiores oportunidades:** (a) extrair o CSS do admin para um arquivo central com **variáveis CSS**; (b) trazer as telas de **auth/Home para a mesma identidade**; (c) padronizar marca/nome; (d) adicionar a fonte Inter; (e) reduzir estilo inline e duplicação de JS; (f) responsividade.
