# 🚀 Portfolio Analytics API

Esta API foi desenvolvida para fornecer análises avançadas de portfólios de investimento, focando em três pilares: **Performance**, **Risco** e **Rebalanceamento Otimizado**.

---

## 🛠️ Como Executar o Projeto
1. Certifique-se de ter o **SDK .NET 8.0** instalado.
2. Clone o repositório e navegue até a pasta `PortfolioAnalyticsApi` onde está o arquivo `PortfolioAnalyticsApi.csproj`.
3. Execute o comando `dotnet run`.
4. Acesse a documentação Swagger em: `https://localhost:5252/swagger`.

---

## 🧪 Como Executar os Testes Unitários
1. Na raiz do repositório, execute o comando `dotnet test`.
2. Valide no terminal o resumo com todos os testes passando.

---

## 📉 Explicação dos Cálculos (Passo a Passo)

### 1. Performance do Portfólio
O objetivo é medir o rendimento e a oscilação do capital.
* **Retorno Total:** Diferença percentual entre o valor investido e o valor atual.
* **Retorno Anualizado:** Projeção do retorno para o período de um ano (365 dias) usando juros compostos.
* **Volatilidade:** Mede o desvio padrão dos retornos diários. É anualizada multiplicando o desvio diário pela raiz quadrada de 252 (dias úteis do mercado).

### 2. Análise de Risco (Risk Analysis)
Avalia se a carteira está eficiente e bem diversificada.
* **Sharpe Ratio:** Mede quanto o portfólio rende acima da Selic para cada 1% de volatilidade.
  * *Cálculo:* `(RetornoAnualizado - TaxaSelic) / Volatilidade`
* **Regras de Risco (Overall Risk):**
  * **Alto (High):** Se um ativo sozinho > 25% da carteira OU um setor > 40%.
  * **Médio (Medium):** Se um ativo está entre 15-25% OU setor entre 25-40%.
  * **Baixo (Low):** Se todas as posições < 15% E setores < 25%.

### 3. Rebalanceamento (Rebalancing)
Sugere ajustes para manter a carteira fiel à estratégia (Target Allocation).
* **Gatilho de 2%:** O sistema só sugere compra/venda se o peso do ativo desviou mais de 2% do alvo.
* **Filtro de Viabilidade:** Não sugerimos operações menores que **R$ 100,00** para evitar que taxas consumam o lucro.
* **Custos:** É calculado automaticamente um custo de **0.3%** sobre cada operação sugerida.
* **Priorização:** O sistema ordena os trades começando pelos maiores desvios.

---

## 🏗️ Decisões Técnicas
* **Arquitetura e Estrutura:**
    * **Minimal APIs:** Em vez de Controllers tradicionais, optei por usar Minimal APIs (.NET 8) para definir os endpoints. Essa abordagem é mais moderna, leve e reduz o boilerplate, mantendo o código mais limpo e direto.
    * **Arquitetura de Serviços (SOLID):** Seguindo os princípios de design SOLID, a lógica de negócio foi segregada em serviços especializados, cada um com uma responsabilidade única (SRP):
        * `PerformanceService`: Orquestra os cálculos de performance.
        * `RiskAnalysisService`: Orquestra a análise de risco.
        * `RebalancingService`: Orquestra as sugestões de rebalanceamento.
    * **Serviço de Domínio para Cálculos (Composição > Herança):** Para evitar duplicação de código (DRY) e acoplamento forte, os algoritmos matemáticos reutilizáveis foram centralizados no `PortfolioCalculatorService`. Este serviço é injetado nos outros, promovendo a composição e facilitando a testabilidade.
* **Gerenciamento de Dados em Memória:**
    * **Fonte de Dados Única:** Para focar nos algoritmos financeiros, optei por carregar o `SeedData.json` em um `InMemoryDataContext` registrado como **Singleton**.
    * **Performance:** Essa abordagem garante que o arquivo JSON seja lido e processado apenas uma vez na inicialização da API. Todas as consultas subsequentes dos repositórios são feitas em coleções na memória, garantindo máxima performance para os endpoints.
* **Modelo de Dados Unificado (Pragmatismo):**
    * **Simplificação Consciente:** Optei por usar um único conjunto de classes para representar os dados em todas as camadas (desserialização, repositórios e serviços).
    * **Foco no Objetivo:** Essa decisão pragmática evitou a complexidade de mapear objetos (ex: Entidades para DTOs), permitindo focar 100% nos algoritmos financeiros, que era o núcleo do desafio. Em um projeto de produção, a separação de modelos seria considerada.
* **Organização dos Testes (Contexto da Entrevista):**
    * **Escolha para este teste:** Os testes unitários foram colocados dentro do próprio projeto da API (`PortfolioAnalyticsApi/Tests`) para manter a estrutura mais enxuta e facilitar a avaliação rápida durante o desafio.
    * **Cenário real recomendado:** Em produção, o ideal é manter testes em projeto separado para evitar acoplamento de dependências de teste no artefato da API, melhorar isolamento no build/deploy e manter CI/CD mais organizado.
* **Tipagem Numérica:**
    * `decimal`: Usado para dinheiro e valores monetários (precisão absoluta).
    * `double`: Usado para taxas, pesos e cálculos estatísticos (performance matemática).
* **Design dos Endpoints e Identificação de Portfólios:**
    * **Identificação por `userId`:** Para os endpoints que analisam um portfólio específico, optei por usar o `userId` como identificador na rota (ex: `/api/portfolios/{userId}/performance`) em vez de um ID de portfólio (como um GUID).
    * **Pragmatismo e Foco no Teste:** Essa decisão foi pragmática, visando simplificar os testes manuais via Swagger. Em vez de primeiro ter que buscar um ID de portfólio para depois usá-lo em outra chamada, o `userId` (`user-001`, `user-002`, etc.) é previsível e fácil de memorizar durante os testes.
    * **Adequação ao Contexto:** Como o projeto não envolve persistência de dados em um banco relacional (onde um ID de portfólio seria a chave primária), o uso do `userId` como chave de busca no contexto de dados em memória é uma abordagem eficiente e perfeitamente adequada para o escopo do desafio.

---

## Análise dos Dados e Limitações

### Volatilidade e Sharpe Ratio

Ao analisar os dados de exemplo fornecidos em `SeedData.json`, foi identificado que os cálculos de **Volatilidade** e, consequentemente, do **Sharpe Ratio** não são realizados para nenhum dos três portfólios (Conservador, Crescimento e Agressivo).

*   **Causa**: A teoria para o cálculo da volatilidade de um portfólio requer a série temporal de valores de *todos* os seus ativos constituintes. Cada um dos portfólios de exemplo contém ao menos um ativo para o qual não há um histórico de preços (`priceHistories`) no arquivo `SeedData.json`.

*   **Comportamento do Sistema**: A implementação atual lida corretamente com esta limitação. O `PortfolioCalculatorService` verifica a disponibilidade do histórico para todos os ativos antes de proceder. Na ausência de dados completos, o serviço retorna `null` para a volatilidade, protegendo a integridade da análise e evitando o cálculo de um indicador de risco que não refletiria a realidade do portfólio.

### Análise de Risco "Overall Risk"

Foi observado que todos os portfólios de exemplo são classificados com risco **"High"**. A análise aprofundada, validada com logs, confirmou que este comportamento está **correto** e é causado por:

*   **Concentração de Posição/Setor**: Os portfólios "Crescimento" e "Agressivo" possuem ativos individuais com peso superior a 25%. O portfólio "Conservador" possui uma concentração no setor "Financial" superior a 40%.

*   **Inconsistência nos Dados de Origem**: É importante notar que o campo `totalInvestment` nos portfólios do `SeedData.json` parece estar defasado e não reflete a soma real do custo das posições. A aplicação **ignora corretamente** este campo e baseia todos os cálculos de peso e risco no valor de mercado atual das posições, garantindo uma análise precisa e revelando o risco real de concentração que poderia passar despercebido.

### Rebalanceamento e Critérios Ambíguos

Na especificação do endpoint de rebalanceamento, o item **"considerar custos vs benefícios"** é ambíguo porque não define como calcular o benefício nem qual limiar de decisão deve ser usado.

Por isso, a implementação segue as regras objetivas explicitamente descritas no desafio (`|desvio| > 2%`, valor mínimo de `R$ 100,00`, custo de `0.3%` e priorização por maior desvio), sem aplicar um critério adicional de benefício que não foi especificado.

---

## 👨‍💻 Autor

Desenvolvido por **[Davi Assunção de Paula]**