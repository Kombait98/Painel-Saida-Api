-- ============================================================================
-- Sistema de Controle de Saída Escolar - Modelo de Dados (PostgreSQL)
-- Banco: painel_saida_db
-- ============================================================================

-- 1. Entidades Estáticas
CREATE TABLE IF NOT EXISTS tipo_pessoa (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS turno (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS tipo_vinculo (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS status_saida (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(50) NOT NULL
);

-- 2. Pessoa
CREATE TABLE IF NOT EXISTS pessoa (
    id SERIAL PRIMARY KEY,
    nome_completo VARCHAR(150) NOT NULL,
    cpf VARCHAR(14) NULL,
    data_nascimento DATE NULL,
    foto TEXT NULL,
    tipo_pessoa_id INT NOT NULL REFERENCES tipo_pessoa(id),
    user_id VARCHAR(100) NULL,
    senha VARCHAR(100) NULL,
    telefone VARCHAR(20) NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 3. Turma
CREATE TABLE IF NOT EXISTS turma (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    ano_letivo INT NOT NULL,
    turno_id INT NOT NULL REFERENCES turno(id),
    professor_pessoa_id INT NULL REFERENCES pessoa(id),
    sala VARCHAR(50) NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 4. Aluno (1 para 0..1 com Pessoa)
CREATE TABLE IF NOT EXISTS aluno (
    id SERIAL PRIMARY KEY,
    pessoa_id INT NOT NULL UNIQUE REFERENCES pessoa(id),
    matricula VARCHAR(50) NOT NULL UNIQUE,
    turma_id INT NOT NULL REFERENCES turma(id),
    pode_sair_sozinho BOOLEAN NOT NULL DEFAULT FALSE,
    observacao_portaria TEXT NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 5. AlunoResponsavel
CREATE TABLE IF NOT EXISTS aluno_responsavel (
    id SERIAL PRIMARY KEY,
    aluno_id INT NOT NULL REFERENCES aluno(id),
    responsavel_pessoa_id INT NOT NULL REFERENCES pessoa(id),
    tipo_vinculo_id INT NOT NULL REFERENCES tipo_vinculo(id),
    responsavel_principal BOOLEAN NOT NULL DEFAULT FALSE,
    pode_retirar BOOLEAN NOT NULL DEFAULT TRUE,
    data_validade TIMESTAMP NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 6. SolicitacaoSaida
CREATE TABLE IF NOT EXISTS solicitacao_saida (
    id SERIAL PRIMARY KEY,
    aluno_id INT NOT NULL REFERENCES aluno(id),
    responsavel_pessoa_id INT NULL REFERENCES pessoa(id),
    porteiro_pessoa_id INT NULL REFERENCES pessoa(id),
    professor_pessoa_id INT NULL REFERENCES pessoa(id),
    data_hora_solicitacao TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_hora_confirmacao_professor TIMESTAMP NULL,
    data_hora_entrega TIMESTAMP NULL,
    status_id INT NOT NULL REFERENCES status_saida(id),
    observacao TEXT NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 7. HistoricoSaida
CREATE TABLE IF NOT EXISTS historico_saida (
    id SERIAL PRIMARY KEY,
    solicitacao_saida_id INT NOT NULL REFERENCES solicitacao_saida(id),
    status_id INT NOT NULL REFERENCES status_saida(id),
    data_hora TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    alterado_por_user_id VARCHAR(100) NULL,
    observacao TEXT NULL
);

-- ============================================================================
-- Carga Inicial (Entidades Estáticas)
-- ============================================================================

INSERT INTO tipo_pessoa (id, nome) VALUES
(1, 'Aluno'),
(2, 'Responsavel'),
(3, 'Professor'),
(4, 'Porteiro'),
(5, 'Administrador')
ON CONFLICT (id) DO NOTHING;

INSERT INTO turno (id, nome) VALUES
(1, 'Matutino'),
(2, 'Vespertino'),
(3, 'Integral'),
(4, 'Noturno')
ON CONFLICT (id) DO NOTHING;

INSERT INTO tipo_vinculo (id, nome) VALUES
(1, 'Mae'),
(2, 'Pai'),
(3, 'TutorLegal'),
(4, 'Avo'),
(5, 'Familiar'),
(6, 'Outro')
ON CONFLICT (id) DO NOTHING;

INSERT INTO status_saida (id, nome) VALUES
(1, 'AguardandoProfessor'),
(2, 'AlunoChamado'),
(3, 'AlunoNaPortaria'),
(4, 'Entregue'),
(5, 'Cancelada'),
(6, 'Recusada')
ON CONFLICT (id) DO NOTHING;
