# Painel de Saída Escolar - API (.NET 8 + PostgreSQL + SignalR)

Backend oficial do **Sistema de Controle de Saída Escolar**, desenvolvido em **C# (.NET 8)** utilizando **Entity Framework Core (Npgsql)** para persistência no **PostgreSQL** e **SignalR** para comunicação em tempo real entre Portaria e Salas de Aula.

---

## 🗄️ Modelo de Dados Implementado

O modelo segue fielmente o diagrama de entidades e relacionamentos:

### Entidades Principais
- **`Pessoa` (`pessoa`)**: `Id`, `NomeCompleto`, `CPF`, `DataNascimento`, `Foto`, `TipoPessoaId (FK)`, `UserId`, `Senha`, `Telefone`, `Ativo`.
- **`Aluno` (`aluno`)**: `Id`, `PessoaId (FK 1:0..1)`, `Matricula`, `TurmaId (FK)`, `PodeSairSozinho`, `ObservacaoPortaria`, `Ativo`.
- **`AlunoResponsavel` (`aluno_responsavel`)**: `Id`, `AlunoId (FK)`, `ResponsavelPessoaId (FK)`, `TipoVinculoId (FK)`, `ResponsavelPrincipal`, `PodeRetirar`, `DataValidade`, `Ativo`.
- **`Turma` (`turma`)**: `Id`, `Nome`, `AnoLetivo`, `TurnoId (FK)`, `ProfessorPessoaId (FK)`, `Sala`, `Ativo`.
- **`SolicitacaoSaida` (`solicitacao_saida`)**: `Id`, `AlunoId (FK)`, `ResponsavelPessoaId (FK)`, `PorteiroPessoaId (FK)`, `ProfessorPessoaId (FK)`, `DataHoraSolicitacao`, `DataHoraConfirmacaoProfessor`, `DataHoraEntrega`, `StatusId (FK)`, `Observacao`, `Ativo`.
- **`HistoricoSaida` (`historico_saida`)**: `Id`, `SolicitacaoSaidaId (FK)`, `StatusId (FK)`, `DataHora`, `AlteradoPorUserId`, `Observacao`.

### Entidades Estáticas (Domínio)
- **`TipoPessoa` (`tipo_pessoa`)**: `1 = Aluno`, `2 = Responsavel`, `3 = Professor`, `4 = Porteiro`, `5 = Administrador`
- **`Turno` (`turno`)**: `1 = Matutino`, `2 = Vespertino`, `3 = Integral`, `4 = Noturno`
- **`TipoVinculo` (`tipo_vinculo`)**: `1 = Mae`, `2 = Pai`, `3 = TutorLegal`, `4 = Avo`, `5 = Familiar`, `6 = Outro`
- **`StatusSaida` (`status_saida`)**:
  - `1 = AguardandoProfessor` (Porteiro chamou o aluno)
  - `2 = AlunoChamado` (Professor confirmou / aluno se arrumando)
  - `3 = AlunoNaPortaria` (Professor liberou o aluno para a portaria)
  - `4 = Entregue` (Porteiro validou a entrega ao responsável)
  - `5 = Cancelada`
  - `6 = Recusada`

---

## 🔌 Endpoints Principais

### Autenticação (`/api/auth`)
- `POST /api/auth/login` — Autentica pelo `UserId` e `Senha` na tabela `Pessoa`, retornando `TipoPessoa` e a `TurmaId` (caso seja professor).

### Turmas e Alunos (`/api/turmas`)
- `GET /api/turmas` — Lista todas as turmas ativas com turno e professor responsável.
- `GET /api/turmas/{id}/alunos` — Lista todos os alunos da turma já combinados com o status da `SolicitacaoSaida` ativa e responsável principal.

### Solicitações de Saída e Histórico (`/api/solicitacoessaida`)
- `GET /api/solicitacoessaida?turmaId={id}` — Lista solicitações ativas.
- `POST /api/solicitacoessaida` — Porteiro abre (ou reenvia) solicitação de saída para um aluno, gravando automaticamente em `HistoricoSaida` e disparando evento em tempo real via SignalR.
- `PATCH /api/solicitacoessaida/{id}/status` — Atualiza o status (`AlunoChamado`, `AlunoNaPortaria`, `Entregue`, etc.), preenche `DataHoraConfirmacaoProfessor` ou `DataHoraEntrega`, registra em `HistoricoSaida` e notifica via SignalR.
- `GET /api/solicitacoessaida/{id}/historico` — Retorna toda a linha do tempo (`HistoricoSaida`) daquela solicitação.

---

## 🚀 Como Rodar

1. Crie ou edite o arquivo `appsettings.Development.json` (ignorado pelo Git) com a sua Connection String do PostgreSQL:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=painel_saida_db;Username=postgres;Password=SUA_SENHA"
     }
   }
   ```
2. Na pasta `painel-saida-api`, execute:
   ```bash
   dotnet run
   ```
3. Na subida, o `DbInitializer` cria automaticamente o banco `painel_saida_db` no PostgreSQL com todas as tabelas e a carga inicial.
4. Acesse o Swagger em `http://localhost:5000/swagger`.
