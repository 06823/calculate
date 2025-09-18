using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CalculatorApp
{
    public partial class Form1 : Form
    {
        /// <summary>最初の値</summary>
        private decimal FirstValue = 0m;

        /// <summary>2番目の値</summary>
        private decimal SecondValue = 0m;

        /// <summary>結果欄の上書き入力フラグ</summary>
        private bool TextOverwrite = false;

        /// <summary>小数点入力済みフラグ</summary>
        private bool NumDot = false;

        /// <summary>エラー状態</summary>
        private bool IsErrorState = false;

        /// <summary>現在の入力を CE でクリアしたか</summary>
        private bool IsClearEntry = false;

        /// <summary>内部の現在表示値（表示文字列と分離）</summary>
        private decimal DisplayValue = Constants.Numeric.INITIAL_VALUE;

        /// <summary>±押下時にフォーマット保持するか</summary>
        private bool PreserveFormatOnToggle = false;

        /// <summary>直近のユーザー生入力（カンマなし＝編集バッファの唯一のソース）</summary>
        private string lastUserTypedRaw = Constants.Numeric.ZERO_VALUE;

        /// <summary>直前が％か</summary>
        private bool lastActionWasPercent = false;

        /// <summary>＝直後に途中式を消したか</summary>
        private bool ClearedExprAfterEqual = false;

        /// <summary>基準フォントサイズ（初期値）</summary>
        private float defaultFontSize;

        /// <summary>途中式欄の基準フォントサイズ（初期値）</summary>
        private float defaultExpressionFontSize;

        /// <summary>±直近押下</summary>
        private bool isNegated = false;

        /// <summary>エラー時に無効化するボタン（＝は含めない）</summary>
        private Button[] DisabledButtonsOnError;

        /// <summary>% などで自動生成された右辺を編集不可にする</summary>
        private bool lockRhsAfterAutoOp = false;

        /// <summary>＝直後%の連打用。基準 r/100（rは＝時の結果）</summary>
        private decimal percentChainFactor = 0m;

        /// <summary>＝直後%の連打中か</summary>
        private bool inPercentChainAfterEqual = false;

        /// <summary>
		/// 演算子の種類を定義する列挙型
		/// </summary>
		private enum OperatorType
        {
            /// <summary>演算なし</summary>
            NON,
            /// <summary>加算</summary>
            ADD,
            /// <summary>減算</summary>
            SUBTRACT,
            /// <summary>乗算</summary>
            MULTIPLY,
            /// <summary>除算</summary>
            DIVIDE,
            /// <summary>パーセント</summary>
            PERCENT
        }

        private enum ErrorCode
        {
            Success,
            Undefined,
            DivideByZero
        }


        /// <summary>現在の演算子種別を保持する変数</summary>
		private OperatorType currentOperatorType = OperatorType.NON;

        /// <summary>
		/// フォームのコンストラクタ
		/// </summary>
        public Form1()
        {
            InitializeComponent();

            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            DisabledButtonsOnError = new Button[]
            {
                btnDot, btnTogglesign, btnPercent, btnPlus,
                btnMinus, btnMultiply, btnDivide
            };
        }

        /// <summary>
		/// フォームの初期化処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
        private void Form1_Load(object sender, EventArgs e)
        {
            textResult.Text = Constants.Numeric.ZERO_VALUE;
            TextOverwrite = true;

            textResult.Font = new Font(textResult.Font.FontFamily, Constants.FontSize.RESULT_DISPLAY_BASE, textResult.Font.Style);
            textExpression.Font = new Font(textExpression.Font.FontFamily, Constants.FontSize.EXPRESSION_DISPLAY_BASE, textExpression.Font.Style);

            defaultFontSize = textResult.Font.Size;
            defaultExpressionFontSize = textExpression.Font.Size;

            textResult.ReadOnly = true;
            textResult.TextAlign = HorizontalAlignment.Right;
            textResult.BorderStyle = BorderStyle.None;

            textExpression.ReadOnly = true;
            textExpression.TextAlign = HorizontalAlignment.Right;
            textExpression.BorderStyle = BorderStyle.None;

            // 内部値の初期同期
            DisplayValue = 0m;
            lastUserTypedRaw = "0";
        }

        /// <summary>
		/// 結果表示欄のフォントサイズを初期化
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void textResult_TextChanged(object sender, EventArgs e)
        {
            AutoFitResultFont();
        }

        private void textExpression_TextChanged(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// 数字ボタン押下時のイベントハンドラー
        /// </summary>
        /// <param name="sender">イベントの発生元</param>
        /// <param name="e">イベントデータ</param>
        private void btnNum_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null)
            {
                return;
            }
            if (IsError())
            {
                ResetCalculatorState();
            }
            OnDigitButton(btn.Text);
        }


        /// <summary>
		/// 小数点キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnDot_Click(object sender, EventArgs e)
        {
            HandleInitialState();
            OnDotButton();
        }

        /// <summary>
		/// 演算子キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnOperation_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var op = OperatorType.NON;

            switch (btn.Text)
            {
                case Constants.Symbol.ADD:
                    op = OperatorType.ADD;
                    break;

                case Constants.Symbol.SUBTRACT:
                    op = OperatorType.SUBTRACT;
                    break;

                case Constants.Symbol.MULTIPLY:
                    op = OperatorType.MULTIPLY;
                    break;

                case Constants.Symbol.DIVIDE:
                    op = OperatorType.DIVIDE;
                    break;
            }

            OnOperatorButton(op);
        }


        /// <summary>
		/// イコールキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnEnter_Click(object sender, EventArgs e)
        {
            OnEqualsButton();
        }

        /// <summary>
		/// ％キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnPercent_Click(object sender, EventArgs e)
        {
            OnPercentButton();
        }


        /// <summary>
		/// クリアエントリーキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClearEntry_Click(object sender, EventArgs e)
        {
            OnClearEntryButton();
        }

        /// <summary>
		/// クリアキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClear_Click(object sender, EventArgs e)
        {
            OnClearButton();
        }

        /// <summary>
		/// 桁下げキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnBack_Click(object sender, EventArgs e)
        {
            OnBackspaceButton();
        }

        /// <summary>
		///サインチェンジキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTogglesign_Click(object sender, EventArgs e)
        {
            OnToggleSignButton();
        }

        /// <summary>
		/// 最前面表示キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTopMost_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
        }

        // ===== 入力処理：編集は raw に対してのみ。内部値は raw を元に更新 =====

        /// <summary>
		/// 数字キー入力のメイン処理。
		/// ユーザーが押した数字を、現在の電卓の状態に応じて処理
		/// </summary>
		/// <param name="digit">入力された数字を表す文字列</param>
        private void OnDigitButton(string digit)
        {
            HandleInitialState();
            SetButtonsEnabled(true);
            lastActionWasPercent = false;

            if (TextOverwrite && currentOperatorType != OperatorType.NON && !string.IsNullOrEmpty(textExpression.Text))
            {
                // 式欄が "A op B" 形式か判定
                var expr = textExpression.Text.Trim();
                var opSym = GetOperatorSymbol(currentOperatorType);
                // 右辺がある場合のみ消す
                if (expr.Contains(opSym))
                {
                    // "A op B" → "A op" にする
					var left = FormatNumberForExpression(FirstValue);
					textExpression.Text = string.Format("{0} {1}", left, opSym);
                }
            }

            if (IsExponentDisplay())
            {
                // 指数表示からの直接追記は上書き開始に切替
                TextOverwrite = true;
                NumDot = false;
                lastUserTypedRaw = "0";
            }

            string currentRaw = lastUserTypedRaw;
            if (!IsInputValid(currentRaw, digit)) return;

            if (TextOverwrite)
            {
                StartNewNumber(digit);
            }
            else
            {
                AppendDigit(digit);
            }

            // raw -> 内部
            decimal dv; // ⬅️ ここで変数を事前に宣言
			if (decimal.TryParse(lastUserTypedRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				DisplayValue = dv;
			}		

            // 画面
            textResult.Text = InsertCommasIfNeeded(lastUserTypedRaw, NumDot);
            IsClearEntry = false;
            PreserveFormatOnToggle = true;
        }

        private void OnDotButton()
        {
            HandleInitialState();
            lastActionWasPercent = false;

            if (NumDot) return;

            if (TextOverwrite)
            {
                lastUserTypedRaw = "0.";
                textResult.Text = lastUserTypedRaw;
                TextOverwrite = false;
            }
            else
            {
                lastUserTypedRaw += ".";
                textResult.Text = lastUserTypedRaw;
            }
            NumDot = true;

            // raw -> 内部
			string stringToParse = lastUserTypedRaw;
			if (stringToParse == "0.")
			{
				stringToParse = "0";
			}

			decimal dv;
			if (decimal.TryParse(stringToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				DisplayValue = dv;
			}

            PreserveFormatOnToggle = true;
        }

        /// <summary>
		/// 小数点キー入力のメイン処理。
		/// 小数点の重複入力を防ぎ、入力状態に応じて表示を更新
		/// </summary>
        private void OnOperatorButton(OperatorType op)
        {
            if (IsError()) { ResetCalculatorState(); return; }

            try
            {
                // ===== 1) 末尾が演算子なら「置換」：右辺未入力(TextOverwrite==true)のときだけ許可 =====
                string curExpr = (textExpression.Text ?? "").Trim();
                if (TextOverwrite && !string.IsNullOrEmpty(curExpr) && !curExpr.EndsWith(Constants.Symbol.EQUAL))
                {
                    string[] ops = { Constants.Symbol.ADD, Constants.Symbol.SUBTRACT, Constants.Symbol.MULTIPLY, Constants.Symbol.DIVIDE };
                    foreach (var o in ops)
                    {
                        if (curExpr.EndsWith(o))
                        {
                            textExpression.Text = curExpr.Substring(0, curExpr.Length - o.Length) + GetOperatorSymbol(op);
                            currentOperatorType = op;

                            lockRhsAfterAutoOp = false;
                            lastActionWasPercent = false;
                            TextOverwrite = true;
                            NumDot = false;
                            return;
                        }
                    }
                }

                // ===== 2) = の直後：現在値を左辺として新規開始 =====
                if (ExpressionEndsWithEqual())
                {
                    FirstValue = GetCurrentValue();
                    SecondValue = Constants.Numeric.INITIAL_VALUE;
                    currentOperatorType = op;

                    UpdateExpressionDisplay(FirstValue, currentOperatorType);
                    DisplayNumber(FirstValue, true);

                    // フラグ類
                    TextOverwrite = true;
                    NumDot = false;
                    lastActionWasPercent = false;
                    PreserveFormatOnToggle = false;
                    lockRhsAfterAutoOp = false;
                    return;
                }

                // ===== 3) % の直後に演算子：右辺を確定して連鎖計算 =====
                if (lastActionWasPercent && currentOperatorType != OperatorType.NON)
                {
                    var cur = GetCurrentValue();          // 右辺（%で置換済み）
                    PerformPendingCalculation(cur);       // A op cur
                    if (IsError()) return;

                    DisplayNumber(FirstValue, true);
                    currentOperatorType = op;
                    UpdateExpressionDisplay(FirstValue, currentOperatorType);

                    // フラグ類
                    TextOverwrite = true;
                    NumDot = false;
                    lastActionWasPercent = false;
                    PreserveFormatOnToggle = false;
                    lockRhsAfterAutoOp = false;
                    return;
                }

                // ===== 4) CE で右辺クリア直後など：演算子だけ差し替え =====
                if (IsClearEntry)
                {
                    // ここを修正: FirstValueを現在値で更新
                    FirstValue = GetCurrentValue();
                    SecondValue = Constants.Numeric.INITIAL_VALUE;
                    currentOperatorType = op;
                    UpdateExpressionDisplay(FirstValue, currentOperatorType);
                    DisplayNumber(FirstValue, true);

                    IsClearEntry = false;
                    TextOverwrite = true;
                    NumDot = false;
                    lastActionWasPercent = false;
                    PreserveFormatOnToggle = false;
                    lockRhsAfterAutoOp = false;
                    return;
                }

                // ===== 5) 右辺未入力で演算子だけ変えたいケース（A op のまま） =====
                if (TextOverwrite && currentOperatorType != OperatorType.NON)
                {
                    currentOperatorType = op;
                    UpdateExpressionDisplay(FirstValue, currentOperatorType);

                    TextOverwrite = true;
                    NumDot = false;
                    lastActionWasPercent = false;
                    PreserveFormatOnToggle = false;
                    lockRhsAfterAutoOp = false;
                    return;
                }

                // ===== 6) 通常ケース：A op B を計算 → op 更新 =====
                var currentValue = GetCurrentValue();     // 右辺 B
                PerformPendingCalculation(currentValue);  // A op B
                if (IsError()) return;

                DisplayNumber(FirstValue, true);
                currentOperatorType = op;
                UpdateExpressionDisplay(FirstValue, currentOperatorType);

                // フラグ類
                TextOverwrite = true;
                NumDot = false;
                lastActionWasPercent = false;
                PreserveFormatOnToggle = false;
                lockRhsAfterAutoOp = false;
            }
            catch (OverflowException)
            {
                SetErrorState(Constants.ErrorMessage.OVERFLOW);
            }
        }


        /// <summary>
		/// イコールキーのメイン処理
		/// 保留中の計算を最終確定し、結果を表示 
		/// </summary>
		private void OnEqualsButton()
        {
            if (ShouldResetOnError())
            {
                return;
            }

            try
            {
                var result = ProcessEqualsLogic();
                if (IsError())
                {
                    return;
                }
                DisplayNumber(result, true);

                PreserveFormatOnToggle = false;
                lastActionWasPercent = false;
            }
            catch (InvalidOperationException ex)
            {
                SetErrorState(ex.Message);
            }
            catch (OverflowException)
            {
                SetErrorState(Constants.ErrorMessage.OVERFLOW);
            }
        }

        /// <summary>
		/// '%'キーが押されたときのイベントハンドラ。
		/// 計算機の現在の状態に基づいて、
		/// パーセント計算を行い、画面の表示を更新
		/// </summary>
        private void OnPercentButton()
        {
            if (ShouldResetOnError()) return;

            // ＝表示中の％ 
            if (ExpressionEndsWithEqual())
            {
                var r = GetCurrentValue();

                if (currentOperatorType == OperatorType.ADD || currentOperatorType == OperatorType.SUBTRACT)
                {
                    // +/− の計算を終えた直後だけ、r% を r に掛けるモード
                    percentChainFactor = r * Constants.Numeric.PERCENT_MULTIPLY; // r/100
                    var v = r * percentChainFactor;   // 1回目: r * (r/100)

                    FirstValue = v;
                    SecondValue = Constants.Numeric.INITIAL_VALUE;
                    currentOperatorType = OperatorType.NON;

                    DisplayNumber(v, true);
                    textExpression.Text = FormatNumberForExpression(v);

                    lastActionWasPercent = true;
                    PreserveFormatOnToggle = false;
                    lockRhsAfterAutoOp = true;
                    inPercentChainAfterEqual = true;  // 以降の％は ×(r/100)
                    return;
                }
                else
                {
                    // ×/÷ の直後は従来通り r/100
                    var v = CalculatePercent(r);

                    FirstValue = v;
                    SecondValue = Constants.Numeric.INITIAL_VALUE;
                    currentOperatorType = OperatorType.NON;

                    DisplayNumber(v, true);
                    textExpression.Text = FormatNumberForExpression(v);

                    lastActionWasPercent = true;
                    PreserveFormatOnToggle = false;
                    lockRhsAfterAutoOp = true;
                    inPercentChainAfterEqual = false;
                    percentChainFactor = 0m;
                    return;
                }
            }

            // ===== ＝直後％チェーンの続き（単独値状態で％連打） =====
            if (currentOperatorType == OperatorType.NON && inPercentChainAfterEqual && percentChainFactor != 0m)
            {
                var cur = GetCurrentValue();          // 直前の表示値
                var v = cur * percentChainFactor;     // 毎回 ×(r/100)

                FirstValue = v;
                SecondValue = Constants.Numeric.INITIAL_VALUE;

                DisplayNumber(v, true);
                textExpression.Text = FormatNumberForExpression(v);

                lastActionWasPercent = true;
                PreserveFormatOnToggle = false;
                lockRhsAfterAutoOp = true;
                return;
            }

            // ===== 二項演算中 A op B での％（従来どおり） =====
            var rhsSource = TextOverwrite ? FirstValue : GetCurrentValue();
            var percent = CalculatePercent(rhsSource);

            decimal replacedB = (currentOperatorType == OperatorType.ADD || currentOperatorType == OperatorType.SUBTRACT)
                                ? FirstValue * percent
                                : percent;

            textExpression.Text = string.Format("{0} {1} {2}",
                FormatNumberForExpression(FirstValue),
                GetOperatorSymbol(currentOperatorType),
                FormatNumberForExpression(replacedB));

            DisplayNumber(replacedB, true);

            lastActionWasPercent = true;
            PreserveFormatOnToggle = false;
            lockRhsAfterAutoOp = true;

            // 二項演算中は＝直後％チェーンではない
            inPercentChainAfterEqual = false;
            percentChainFactor = 0m;
        }

        private decimal lastValueBeforeClearEntry = 0m;

        /// <summary>
		/// クリアエントリーキー入力の処理。式は維持し、表示を 0 に戻す。
		/// </summary>
        private void OnClearEntryButton()
        {
            if (ShouldResetOnError())
            {
                SetButtonsEnabled(true);
                return;
            }

            var currentExpression = textExpression.Text != null ? textExpression.Text.Trim() : string.Empty;

            // ％で右辺を置換した直後に CE → 「A op」に戻し、結果は 0（上書き開始）
            if (!ExpressionEndsWithEqual() && currentOperatorType != OperatorType.NON && lastActionWasPercent)
            {
                DisplayZeroResult();
                textExpression.Text = string.Format("{0} {1}",
                    FormatNumberForExpression(FirstValue),
                    GetOperatorSymbol(currentOperatorType));
                lastActionWasPercent = false;
                lockRhsAfterAutoOp = false;
                return;
            }

            // negate(result) 等で単独結果を表示中（編集ロック中）に CE → 全消去で 0
            if (lockRhsAfterAutoOp && currentOperatorType == OperatorType.NON && !ExpressionEndsWithEqual())
            {
                DisplayZeroResult();
                textExpression.Text = "";
                lockRhsAfterAutoOp = false;
                return;
            }

            if (ExpressionEndsWithEqual())
            {
                if (HasBinaryOperatorInExpression(currentExpression))
                {
                    ResetCalculatorState();
                    SetButtonsEnabled(true);
                    return;
                }

                DisplayZeroResult();
                ResetCalculationValues();
                return;
            }
            lastValueBeforeClearEntry = FirstValue;
            ClearCurrentEntry();
        }

        /// <summary>
		/// 数式列に演算子が含まれているかを判定
		/// ' = '記号より前の部分のみを検査し、加算、減算、乗算、除算のいずれかが含まれていればtrueを返す
		/// </summary>
		/// <param name="expr">検査対象となる数式</param>
		/// <returns>演算子が含まれていればtrue、そうでなければfalse</returns>
		private bool HasBinaryOperatorInExpression(string expr)
        {
            if (string.IsNullOrEmpty(expr))
            {
                return false;
            }

            int eq = expr.LastIndexOf(Constants.Symbol.EQUAL);
            string body = (eq >= 0) ? expr.Substring(0, eq) : expr;

            return body.Contains(Constants.Symbol.ADD) ||
                   body.Contains(Constants.Symbol.SUBTRACT) ||
                   body.Contains(Constants.Symbol.MULTIPLY) ||
                   body.Contains(Constants.Symbol.DIVIDE);
        }

        /// <summary>
		/// 電卓の表示を'0'に設定
		/// </summary>
        private void DisplayZeroResult()
        {
            textResult.Text = Constants.Numeric.ZERO_VALUE;
            TextOverwrite = true;
            NumDot = false;

            // 内部もゼロに同期
            DisplayValue = 0m;
            lastUserTypedRaw = "0";

            PreserveFormatOnToggle = false;
            lastActionWasPercent = false;
        }

        /// <summary>
		/// 計算状態を初期値に戻す
		/// </summary>
        private void ResetCalculationValues()
        {
            FirstValue = Constants.Numeric.INITIAL_VALUE;
            SecondValue = Constants.Numeric.INITIAL_VALUE;
            currentOperatorType = OperatorType.NON;
        }

        /// <summary>
		/// 現在の数値をクリアし、表示を0に戻す
		/// </summary>
        private void ClearCurrentEntry()
        {
            IsClearEntry = true;
            DisplayZeroResult();
        }

        /// <summary>
		/// クリアキーの処理。全状態を初期化する。
		/// </summary>
        private void OnClearButton()
        {
            ResetAllState();
        }

        /// <summary>
		/// 桁下げキー入力の処理
		/// 末尾1文字削除を行う。
		/// </summary>
        private void OnBackspaceButton()
        {
            if (ShouldResetOnError())
            {
                SetButtonsEnabled(true);
                return;
            }

            if (ExpressionEndsWithEqual())
            {
                textExpression.Text = "";
                TextOverwrite = true;
                NumDot = false;
                PreserveFormatOnToggle = false;
                lastActionWasPercent = false;
                ClearedExprAfterEqual = true;

                // 編集バッファと内部の初期化
                lastUserTypedRaw = "0";
                DisplayValue = 0m;
                return;
            }

            if (ClearedExprAfterEqual)
            {
                return;
            }

            if (IsExponentDisplay())
            {
                // 指数表示からのBackspaceは新規入力開始
                TextOverwrite = true;
                NumDot = false;
                textResult.Text = Constants.Numeric.ZERO_VALUE;

                lastUserTypedRaw = "0";
                DisplayValue = 0m;

                PreserveFormatOnToggle = false;
                lastActionWasPercent = false;
                return;
            }

            if (TextOverwrite)
            {
                return;
            }

            // raw を1文字削る
            if (lastUserTypedRaw.Length > 0)
            {
                string newRaw = lastUserTypedRaw.Substring(0, lastUserTypedRaw.Length - 1);
                if (string.IsNullOrEmpty(newRaw) || newRaw == "-")
                {
                    lastUserTypedRaw = "0";
                    TextOverwrite = true;
                    NumDot = false;
                }
                else
                {
                    lastUserTypedRaw = newRaw;
                    NumDot = lastUserTypedRaw.Contains(".");
                }
            }
            else
            {
                ResetCalculatorState();
                return;
            }

            // raw -> 内部
			decimal dv; 

			if (decimal.TryParse(lastUserTypedRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				DisplayValue = dv;
			}

            // 画面
            textResult.Text = InsertCommasIfNeeded(lastUserTypedRaw, NumDot);

            PreserveFormatOnToggle = true;
            lastActionWasPercent = false;
        }

        /// <summary>
		/// イコールキー入力直後の サインチェンジキーを入力したとき negate(...) の入れ子表記で更新する。
		/// 例）"100 =" → "negate(100) " → さらに ± → "negate(negate(100))"
		/// </summary>
		private void UpdateExpressionForToggleSign()
        {
            string expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

            if (expr.EndsWith(Constants.Symbol.EQUAL))
            {
                int eq = expr.LastIndexOf(Constants.Symbol.EQUAL);
                string body = (eq >= 0 ? expr.Substring(0, eq) : expr).Trim();
                if (string.IsNullOrEmpty(body)) body = FormatNumberForExpression(FirstValue);

                textExpression.Text = Constants.SpecialDisplay.NEGATE_FUNCTION + "(" + body + ")";
                return;
            }

            if (expr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
            {
                textExpression.Text = Constants.SpecialDisplay.NEGATE_FUNCTION + "(" + expr + ")";
            }
        }

        /// <summary>
		/// サインチェンジキーの処理。ユーザー入力の見た目保持（末尾ゼロ維持）と、
		/// ＝直後の negate(...) 表記更新に対応する。
		/// </summary>
        private void OnToggleSignButton()
        {
            if (ShouldResetOnError())
            {
                return;
            }

            // イコール直後またはnegateで始まる場合は入れ子構造にする
            string expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);
            bool isNegateExpr = expr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal);

            if (ExpressionEndsWithEqual() || isNegateExpr)
            {
                var v = -GetCurrentValue();
                DisplayNumber(v, true);
                UpdateExpressionForToggleSign();
                PreserveFormatOnToggle = false;
                lastActionWasPercent = false;
                lockRhsAfterAutoOp = true;   // 編集ロック
                return;
            }

            // A op の直後（右辺未入力）: B := negate(A)
            if (currentOperatorType != OperatorType.NON && TextOverwrite)
            {
                var a = FirstValue;
                var b = -a;

                textExpression.Text = string.Format("{0} {1} {2}",
                    FormatNumberForExpression(a),
                    GetOperatorSymbol(currentOperatorType),
                    string.Format("{0}({1})",
                        Constants.SpecialDisplay.NEGATE_FUNCTION,
                        FormatNumberForExpression(a)));

                DisplayValue = b;
                DisplayNumber(b, true);       // 上書き開始
                PreserveFormatOnToggle = false;
                lastActionWasPercent = false;
                lockRhsAfterAutoOp = true;    // 右辺は編集不可
                return;
            }

            // それ以外：編集中の数値を単純反転（編集は継続可）
            DisplayValue = -GetCurrentValue();
            DisplayNumber(DisplayValue, false);
            PreserveFormatOnToggle = false;
            lastActionWasPercent = false;
        }

        /// <summary>
		/// 新しい数値入力を開始する。上書きモードを解除し、小数点フラグを更新。
		/// </summary>
		/// <param name="digit">新しい数値の最初の桁を表す文字列。</param>
        private void StartNewNumber(string digit)
        {
            lastUserTypedRaw = digit;
            textResult.Text = digit;
            TextOverwrite = false;
            NumDot = (digit == ".");

            // raw -> 内部
			string stringToParse = lastUserTypedRaw;

			// 入力値が "." の場合、"0"として扱う
			if (stringToParse == ".")
			{
				stringToParse = "0";
			}

			// 修正後: 変数を事前に宣言
			decimal dv;
			if (decimal.TryParse(stringToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				DisplayValue = dv;
			}
        }

        /// <summary>
		/// 既存の入力の末尾に数字を追加する。
		/// </summary>
		/// <param name="digit">追加する数字を表す文字列。</param>
        private void AppendDigit(string digit)
        {
            lastUserTypedRaw += digit;
            textResult.Text = InsertCommasIfNeeded(lastUserTypedRaw, NumDot);

            decimal dv; 
			if (decimal.TryParse(lastUserTypedRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				DisplayValue = dv;
			}
        }


        /// <summary>
		/// ユーザーの入力が有効であるか検証
		/// 最大表示桁数の超過、重複したゼロ、不適切な小数点の入力などをチェック
		/// </summary>
		/// <param name="currentText">現在の表示。</param>
		/// <param name="digit">新たに入力された数字または記号。</param>
		/// <returns>入力が有効であればtrue、そうでなければfalse。</returns>
		private bool IsInputValid(string currentText, string digit)
        {
            bool startsWithZeroDot = currentText.StartsWith("0.") || currentText.StartsWith("-0.");
            int maxDigits = startsWithZeroDot ? Constants.Numeric.MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO : Constants.Numeric.MAX_INTEGER_DISPLAY_DIGITS;

            string nextText = TextOverwrite ? digit : currentText + digit;
            int nextLength = nextText.Replace(".", "").Replace("-", "").Length;

            if (nextLength > maxDigits)
            {
                return false;
            }

            if (!TextOverwrite && currentText == Constants.Numeric.ZERO_VALUE && digit == Constants.Numeric.ZERO_VALUE && !NumDot)
            {
                return false;
            }

            return true;
        }


        /// <summary>
		/// 指定された演算子に基づき、演算子の記号を返す。
		/// </summary>
		/// <param name="type">演算子</param>
		/// <returns>指定された演算子に対応する記号</returns>
		private string GetOperatorSymbol(OperatorType type)
        {
            switch (type)
            {
                case OperatorType.ADD:
                    {
                        return Constants.Symbol.ADD;
                    }
                case OperatorType.SUBTRACT:
                    {
                        return Constants.Symbol.SUBTRACT;
                    }
                case OperatorType.MULTIPLY:
                    {
                        return Constants.Symbol.MULTIPLY;
                    }
                case OperatorType.DIVIDE:
                    {
                        return Constants.Symbol.DIVIDE;
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        /// <summary>
		/// 左辺と右辺の値を指定された演算子タイプに基づいて計算する
		/// </summary>
		/// <param name="left">左辺の値</param>
		/// <param name="right">右辺の値</param>
		/// <param name="type">演算子のタイプ</param>
		/// <returns>計算結果</returns>
		private decimal Calculate(decimal left, decimal right, OperatorType type)
        {
            switch (type)
            {
                case OperatorType.ADD:
                    {
                        return left + right;
                    }
                case OperatorType.SUBTRACT:
                    {
                        return left - right;
                    }
                case OperatorType.MULTIPLY:
                    {
                        return left * right;
                    }
                case OperatorType.DIVIDE:
                    {
                        return left / right;
                    }
                default:
                    {
                        return right;
                    }
            }
        }


        /// <summary>
		/// 保留中の演算を解決する。未選択なら左辺を現在値に更新し、0除算や0÷0 はエラー化する。
		/// </summary>
		private void PerformPendingCalculation(decimal currentValue)
        {
            if (ExpressionEndsWithEqual() || currentOperatorType == OperatorType.NON)
            {
                FirstValue = currentValue;
            }
            else
            {
                if (currentOperatorType == OperatorType.DIVIDE && currentValue == Constants.Numeric.INITIAL_VALUE)
                {
                    if (FirstValue == Constants.Numeric.INITIAL_VALUE)
                    {
                        SetErrorState(Constants.ErrorMessage.UNDEFINED);
                    }
                    else
                    {
                        SetErrorState(Constants.ErrorMessage.DIVIDE_BY_ZERO);
                    }
                    return;
                }

                decimal result = Calculate(FirstValue, currentValue, currentOperatorType);
                FirstValue = result;
            }
        }

        /// <summary>
		/// 途中式表示を更新する
		/// </summary>
		private void UpdateExpressionDisplay(decimal value, OperatorType type)
        {
            string op = GetOperatorSymbol(type);

            string expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);
            if (expr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
            {
                textExpression.Text = string.Format("{0} {1}", expr, op);
                return;
            }

            // 途中式欄も結果欄と同じ丸め・整形を通す
            string displayStr;
            decimal abs = Math.Abs(value);

            if (abs != 0m && (abs < Constants.Numeric.SCI_SMALL_THRESHOLD || abs >= Constants.Numeric.SCI_LARGE_THRESHOLD))
            {
                displayStr = FormatExponential(value);
            }
            else
            {
                var rounded = RoundResult(value);
                displayStr = FormatNumberForDisplay(rounded);
            }

            textExpression.Text = string.Format("{0} {1}", displayStr, op);
        }

        /// <summary>
		///イコールキーの処理
		/// </summary>
        private decimal ProcessEqualsLogic()
        {
            decimal currentValue = GetCurrentValue();
            bool isFirstEqual = !ExpressionEndsWithEqual();

            if (currentOperatorType == OperatorType.NON)
            {
                SecondValue = currentValue;
                textExpression.Text = string.Format("{0} {1}",
                    FormatNumberForExpression(currentValue), Constants.Symbol.EQUAL);
                FirstValue = currentValue;
                return currentValue;
            }

            decimal left, right;

            if (isFirstEqual)
            {
                left = FirstValue;
                right = currentValue;
                SecondValue = currentValue;
            }
            else
            {
                left = FirstValue;
                right = SecondValue;
            }

            if (currentOperatorType == OperatorType.DIVIDE && right == Constants.Numeric.INITIAL_VALUE)
            {
                if (left == Constants.Numeric.INITIAL_VALUE)
                    throw new InvalidOperationException(Constants.ErrorMessage.UNDEFINED);
                else
                    throw new InvalidOperationException(Constants.ErrorMessage.DIVIDE_BY_ZERO);
            }

            decimal result = Calculate(left, right, currentOperatorType);
            FirstValue = result;

            string opSym = GetOperatorSymbol(currentOperatorType);
            string leftExpr = FormatNumberForExpression(left);
            string rightExpr = FormatNumberForExpression(right);

            string curr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

            if (!string.IsNullOrEmpty(curr) &&
                !curr.EndsWith(Constants.Symbol.EQUAL) &&
                curr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
            {
                if (curr.EndsWith(opSym))
                    textExpression.Text = curr + " " + rightExpr + " " + Constants.Symbol.EQUAL;
                else
                    textExpression.Text = curr + " " + Constants.Symbol.EQUAL;
            }
            else
            {
                textExpression.Text = string.Format("{0} {1} {2} {3}",
                    leftExpr, opSym, rightExpr, Constants.Symbol.EQUAL);
            }

            return result;
        }

        /// <summary>
		/// 現在値を％形式に変換する。
		/// </summary>
		private decimal CalculatePercent(decimal value)
        {
            return value * Constants.Numeric.PERCENT_MULTIPLY;
        }



        // ===== 表示処理 =====

        private void DisplayNumber(decimal value, bool overwrite)
        {
            string s;

            // ここを修正：FormatNumberForDisplayに一任
            var rounded = RoundResult(value);
            s = FormatNumberForDisplay(rounded);

            textResult.Text = s;
            if (!IsExponentDisplay()) UpdateTextResultWithCommas();

            TextOverwrite = overwrite;
            NumDot = false;

            // 内部（DisplayValue）にも反映（※指数文字列は一切パースしない）
            DisplayValue = value;

            PreserveFormatOnToggle = false;
            lastUserTypedRaw = IsExponentDisplay()
                ? DisplayValue.ToString("0.#############################", CultureInfo.InvariantCulture)
                : textResult.Text.Replace(",", "");
        }

        private string InsertCommasIfNeeded(string raw, bool numDot)
        {
            // 生の編集テキストに 3桁区切り
            if (string.IsNullOrEmpty(raw) || raw == "-" || (raw == "0" && !numDot)) return raw;

            bool neg = raw.StartsWith("-");
            if (neg) raw = raw.Substring(1);

            int dot = raw.IndexOf('.');
            string intPart = dot >= 0 ? raw.Substring(0, dot) : raw;
            string fracPart = dot >= 0 ? raw.Substring(dot + 1) : "";

            decimal iv;
            if (decimal.TryParse(intPart, NumberStyles.Number, CultureInfo.InvariantCulture, out iv))
            {
                string intFmt = iv.ToString("#,##0", CultureInfo.InvariantCulture);
                string newText = (dot >= 0) ? (intFmt + "." + fracPart) : intFmt;
                if (neg) newText = "-" + newText;
                return newText;
            }
            return raw;
        }

        private void UpdateTextResultWithCommas()
        {
            if (IsError()) return;
            if (IsExponentDisplay()) return;

            string raw = textResult.Text.Replace(",", "");
            bool hasDot = raw.Contains(".");
            string formatted = InsertCommasIfNeeded(raw, hasDot);

            if (formatted != textResult.Text)
            {
                int fromEnd = textResult.Text.Length - textResult.SelectionStart;
                textResult.Text = formatted;
                textResult.SelectionStart = Math.Max(0, textResult.Text.Length - fromEnd);
            }
        }

        private string FormatNumberForExpression(decimal value)
        {
            var rounded = RoundResult(value);
            return FormatNumberForDisplay(rounded);
        }

        // === 新規: 10 のべき乗（負の指数にも対応） ===
        private static decimal Pow10(int k)
        {
            if (k == 0) return 1m;
            decimal p = 1m;
            if (k > 0) { for (int i = 0; i < k; i++) p *= 10m; }
            else { for (int i = 0; i < -k; i++) p /= 10m; }
            return p;
        }

        // === 新規: |x| の 10 進指数を返す（正規化して 1 <= |mantissa| < 10 となる指数）===
        private static int DecimalBase10Exponent(decimal x)
        {
            decimal ax = Math.Abs(x);
            if (ax == 0m) return 0;

            if (ax >= 1m)
            {
                string s = decimal.Truncate(ax).ToString(CultureInfo.InvariantCulture);
                return s.Length - 1;
            }
            else
            {
                string s = ax.ToString("0.#############################", CultureInfo.InvariantCulture);
                int dot = s.IndexOf('.');
                int zeros = 0;
                for (int i = dot + 1; i < s.Length && s[i] == '0'; i++) zeros++;
                return -(zeros + 1); // 0.000123 → -4
            }
        }

        // === 新規: 有効桁 n で四捨五入（AwayFromZero）===
        private static decimal RoundToSignificantDigits(decimal x, int n)
        {
            if (x == 0m) return 0m;
            int exp = DecimalBase10Exponent(x);        // 10^exp の桁に 1つ目の有効桁
            int scale = n - 1 - exp;                   // 小数点以下で丸めたい桁数

            if (scale < 0)
            {
                // 整数部側で丸め
                int k = -scale;
                return Math.Round(x / Pow10(k), 0, MidpointRounding.AwayFromZero) * Pow10(k);
            }
            else
            {
                // scale > 28 の場合は 28 で丸めて、そのまま返す
                int safeScale = scale > 28 ? 28 : scale;
                decimal rounded = Math.Round(x, safeScale, MidpointRounding.AwayFromZero);
                return rounded;
            }
        }

        // === 差し替え: 指数表示の生成（Windows 電卓寄せ: 有効桁 17 / 整数仮数は "2." 形式）===
        private string FormatExponential(decimal value)
        {
            const int SIG = Constants.Numeric.EXP_SIGNIFICANT_DIGITS; // 16


            if (value == 0m) return "0";

            // 1) 値そのものを 17 有効桁で丸め
            decimal rounded = RoundToSignificantDigits(value, SIG);

            // 2) 正規化
            int exp = DecimalBase10Exponent(rounded);
            decimal mant = rounded / Pow10(exp);

            // 3) 仮数が 10 に到達した場合の再正規化
            if (Math.Abs(mant) >= 10m)
            {
                mant /= 10m;
                exp += 1;
            }

            // 4) 仮数の文字列（整数なら末尾に '.' を付与）
            string mantStr;
            decimal mantAbsTrunc = decimal.Truncate(Math.Abs(mant));
            if (Math.Abs(mant) == mantAbsTrunc)
            {
                mantStr = (mant >= 0 ? "" : "-") + mantAbsTrunc.ToString("0", CultureInfo.InvariantCulture) + ".";
            }
            else
            {
                mantStr = mant.ToString("0.#############################", CultureInfo.InvariantCulture).TrimEnd('0');
            }

            // 5) 指数部
            string expStr = (exp >= 0 ? "+" : "") + exp.ToString(CultureInfo.InvariantCulture);

            return mantStr + "e" + expStr;
        }

        private string FormatNumberForDisplay(decimal value)
        {
            decimal abs = Math.Abs(value);
            if (abs == 0m) return Constants.Numeric.ZERO_VALUE;

            string fixedStr = value.ToString("0.#############################", CultureInfo.InvariantCulture);

            int dot = fixedStr.IndexOf('.');
            bool neg = (fixedStr[0] == '-');
            int intLen = (dot >= 0 ? dot : fixedStr.Length) - (neg ? 1 : 0);

            if (abs < 1m)
            {
                int leadingZeros = 0;
                for (int i = dot + 1; i < fixedStr.Length && fixedStr[i] == '0'; i++) leadingZeros++;

                int significantDigits = 0;
                for (int i = dot + 1 + leadingZeros; i < fixedStr.Length; i++)
                    if (char.IsDigit(fixedStr[i])) significantDigits++;

                // 有効数字が17桁以内なら小数表示
                if (significantDigits <= Constants.Numeric.MAX_SIGNIFICANT_DIGITS)
                    return fixedStr;

                // それ以外は指数表示
                return FormatExponential(value);
            }
            else
            {
                if (intLen > Constants.Numeric.MAX_INTEGER_DISPLAY_DIGITS)
                    return FormatExponential(value);
                return fixedStr;
            }
        }

        // ====== 値取得・丸め ======
        private decimal GetCurrentValue()
		{
			return DisplayValue;
		}

        private decimal RoundResult(decimal value)
        {
            decimal abs = Math.Abs(value);
            if (abs == 0m) return 0m;

            // 指数レンジは事前丸め禁止（表示でのみ丸め/指数化）
            if (abs < Constants.Numeric.SCI_SMALL_THRESHOLD || abs >= Constants.Numeric.SCI_LARGE_THRESHOLD)
                return value;

            if (abs > 0m && abs < 1m)
                return Math.Round(value, 16, MidpointRounding.AwayFromZero);

            if (abs >= 1m)
            {
                string integerPartStr = Math.Floor(abs).ToString(CultureInfo.InvariantCulture);
                int integerLength = integerPartStr.Length;
                int decimalPlacesToRound = 16 - integerLength;
                if (decimalPlacesToRound >= 0)
                    return Math.Round(value, decimalPlacesToRound, MidpointRounding.AwayFromZero);
            }
            return value;
        }

        private bool IsError()
		{
			return IsErrorState;
		}

		private bool ExpressionEndsWithEqual()
		{
			return textExpression.Text.Length > 0 && textExpression.Text.EndsWith(Constants.Symbol.EQUAL);
		}
        private bool IsExponentDisplay()
        {
            string t = textResult.Text;
            return (t.IndexOf('e') >= 0 || t.IndexOf('E') >= 0);
        }

        private bool ShouldResetOnError()
        {
            if (IsErrorState)
            {
                ResetCalculatorState();
                return true;
            }
            return false;
        }

        private void AutoFitResultFont()
        {
            float size = defaultFontSize;
            FontFamily family = textResult.Font.FontFamily;
            FontStyle style = textResult.Font.Style;

            while (size > Constants.FontSize.MIN_LIMIT)
            {
                using (Font trial = new Font(family, size, style))
                {
                    Size proposed = new Size(int.MaxValue, int.MaxValue);
                    TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                    Size sz = TextRenderer.MeasureText(textResult.Text, trial, proposed, flags);

                    if (sz.Width <= textResult.ClientSize.Width)
                    {
                        if (Math.Abs(textResult.Font.Size - size) > 0.1f)
                        {
                            Font old = textResult.Font;
                            textResult.Font = new Font(family, size, style);
                            old.Dispose();
                        }
                        return;
                    }
                }
                size -= Constants.FontSize.SIZE_EPSILON;
            }

            if (Math.Abs(textResult.Font.Size - Constants.FontSize.MIN_LIMIT) > Constants.FontSize.REDUCTION_STEP)
            {
                Font oldFinal = textResult.Font;
                textResult.Font = new Font(family, Constants.FontSize.MIN_LIMIT, style);
                oldFinal.Dispose();
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (Button btn in DisabledButtonsOnError) btn.Enabled = enabled;
        }

        private void SetErrorState(string message)
        {
            textResult.Text = message;

            float sz = Constants.FontSize.ERROR_MESSAGE;
            if (sz < Constants.FontSize.MIN_LIMIT) sz = Constants.FontSize.MIN_LIMIT;
            textResult.Font = new Font(textResult.Font.FontFamily, sz, textResult.Font.Style);

            IsErrorState = true;
            SetButtonsEnabled(false);
        }

        private void HandleInitialState()
        {
            if (IsErrorState || ExpressionEndsWithEqual())
            {
                ResetCalculatorState();
                ClearedExprAfterEqual = false;
            }
        }

        private void ResetAllState()
        {
            ResetCalculatorState();
            SetButtonsEnabled(true);
        }

        private void ResetCalculatorState()
        {
            InitializeValues();
            ClearTextFields();
            ResetFlags();
            ResetFonts();
            AutoFitResultFont();
        }

        private void InitializeValues()
        {
            FirstValue = Constants.Numeric.INITIAL_VALUE;
            SecondValue = Constants.Numeric.INITIAL_VALUE;
            currentOperatorType = OperatorType.NON;
            DisplayValue = 0m;
            lastUserTypedRaw = "0";
            percentChainFactor = 0m;
        }

        private void ClearTextFields()
        {
            textExpression.Text = "";
            textResult.Text = Constants.Numeric.ZERO_VALUE;
        }

        private void ResetFlags()
        {
            TextOverwrite = true;
            NumDot = false;
            IsErrorState = false;
            IsClearEntry = false;
            lastActionWasPercent = false;
            ClearedExprAfterEqual = false;
            inPercentChainAfterEqual = false;
        }

        private void ResetFonts()
        {
            textResult.Font = new Font(textResult.Font.FontFamily, defaultFontSize, textResult.Font.Style);
            textExpression.Font = new Font(textExpression.Font.FontFamily, defaultExpressionFontSize, textExpression.Font.Style);
        }


        /// <summary>アプリ全体で使用する定数</summary>
        internal static class Constants
        {
            internal static class FontSize
            {
                internal const float ERROR_MESSAGE = 20.0f;
                internal const float RESULT_DISPLAY_BASE = 36f;
                internal const float EXPRESSION_DISPLAY_BASE = 10f;
                internal const float MIN_LIMIT = 14f;
                internal const float REDUCTION_STEP = 0.5f;
                internal const float SIZE_EPSILON = 0.1f;
            }

            internal static class Symbol
            {
                internal const string ADD = "+";
                internal const string SUBTRACT = "-";
                internal const string MULTIPLY = "×";
                internal const string DIVIDE = "÷";
                internal const string EQUAL = "=";
            }

            internal static class Numeric
            {
                internal const decimal INITIAL_VALUE = 0m;
                public const string ZERO_VALUE = "0";
                internal const decimal PERCENT_MULTIPLY = 0.01m;

                internal const int MAX_INTEGER_DISPLAY_DIGITS = 16;
                internal const int MAX_FRACTION_DISPLAY_DIGITS_LEADING_ZERO = 17;

                // 固定小数の許容小数桁（0.x の尾部）
                public const int MAX_TOTAL_FRACTION_DIGITS = 16;

                // 有効桁数（指数表示用）
                public const int MAX_SIGNIFICANT_DIGITS = 17;

                // 指数切替しきい値（Windows 電卓寄せ）
                internal static readonly decimal SCI_SMALL_THRESHOLD = 1e-9m;
                internal static readonly decimal SCI_LARGE_THRESHOLD = 1e16m;

                public const int EXP_SIGNIFICANT_DIGITS = 16; // 指数表示時の有効桁（Windows電卓寄せ）

            }

            internal static class ErrorMessage
            {
                internal const string OVERFLOW = "計算範囲を超えました";
                internal const string DIVIDE_BY_ZERO = "0で割ることはできません";
                internal const string UNDEFINED = "結果が定義されていません";
            }

            internal static class SpecialDisplay
            {
                internal const string NEGATE_FUNCTION = "negate";
            }
        }
    }
}
