using System;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CalculatorApp
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 最初の値を保持する変数
        /// </summary>
        private decimal FirstValue = 0;

        /// <summary>
        /// 2番目の値を保持する変数
        /// </summary>
        private decimal SecondValue = 0;

        /// <summary>
        /// テキストボックスの上書きモードを示すフラグ
        /// </summary>
        private bool TextOverwrite = false;

        /// <summary>
        /// 小数点が入力されているかを示すフラグ。
        /// </summary>
        private bool NumDot = false;

        /// <summary>
        /// 加算演算子の記号
        /// </summary>
        private const string AddSymbol = "+";

        /// <summary>
        /// 減算演算子の記号
        /// </summary>
        private const string SubtractSymbol = "-";

        /// <summary>
        /// 乗算演算子の記号
        /// </summary>
        private const string MultiplySymbol = "×";

        /// <summary>
        /// 除算演算子の記号</summary>
        private const string DivideSymbol = "÷";

        /// <summary>
        /// 等号演算子の記号
        /// </summary>
        private const string EqualSymbol = "=";

        /// <summary>
        /// 初期値:0
        /// </summary>
        private const decimal InitialValue = 0m;

        /// <summary>
        /// 表示値:0
        /// </summary>
        private const string ZeroValue = "0";

        /// <summary>
        /// ％を小数に変換する乗数
        /// </summary>
        private const decimal PercentMultiplier = 0.01m;

        /// <summary>
        /// エラーメッセージフォントサイズ
        /// </summary>
        private const float ErrorFontSize = 20.0f;

        /// <summary>
        /// オーバフローが発生したときのエラーメッセージ
        /// </summary>
        private const string ErrMsgOverflow = "計算範囲を超えました";

        /// <summary>
        /// 0除算が発生したときのエラーメッセージ
        /// </summary>
        private const string ErrMsgDivZero = "0で割ることはできません";

        /// <summary>
        /// 0÷0が行われた時のエラーメッセージ
        /// </summary>
        private const string ErrMsgUndefined = "結果が定義されていません"; 

        /// <summary>
        /// サインチェンジキーを入力した際に表示される途中結果表示欄のnegate
        /// </summary>
        private const string NegateFuncName = "negate";

		private bool isNegated = false;

        /// <summary>
        /// 表示桁数
        /// </summary>
        private const int DisplayMaxIntegerDigits = 16;

        /// <summary>
        /// 0.から始まる場合の表示桁数
        /// </summary>
        private const int DisplayMaxFractionDigitsLeadingZero = 17;

        /// <summary>
        /// 計算結果表示欄の基準フォントサイズ
        /// </summary>
        private const float WinResultBaseSize = 36f; 

        /// <summary>
        /// 途中計算結果欄の基準フォントサイズ
        /// </summary>
        private const float WinExprBaseSize = 10f;    

        /// <summary>
        /// フォントの下限サイズ
        /// </summary>
        private const float WinMinFontSize = 14f;      

        /// <summary>
        /// フォントの縮小幅
        /// </summary>
        private const float WinFontStep = 0.5f;        

        /// <summary>
        /// 計算結果表示欄の現在のフォントサイズ
        /// </summary>
        private float defaultFontSize;        
      
        /// <summary>
        /// 途中計算表示欄の現在のフォントサイズ
        /// </summary>
        private float defaultExpressionFontSize;  
  
        /// <summary>
        /// エラー判定フラグ
        /// </summary>
        private bool IsErrorState = false;

		/// <summary>
		/// 現在の表示内容をクリアして新しい値を入力するかどうかを示すフラグ
		/// </summary>
        private bool IsClearEntry = false;

		/// <summary>
		/// 電卓の画面に現在表示されている数値を保持
		/// </summary>
        private decimal displayValue = InitialValue;

        /// <summary>
        /// サインチェンジキーを押したときの、末端のゼロや小数点の状態を保持するか判定するためのフラグ
        /// </summary>
        private bool preserveFormatOnToggle = false;    
		
		/// <summary>
		/// 入力した文字列を保持
		/// </summary>
        private string lastUserTypedRaw = ZeroValue;      

		/// <summary>
		/// 直前の操作がパーセントキーだったかどうかを示すフラグ
		/// </summary>
        private bool lastActionWasPercent = false;

        /// <summary>
		/// エラー時に操作無効なキー
        /// </summary>
        private Button[] DisabledButtonsOnError;

        /// <summary>
        /// 
        /// </summary>
        private bool clearedExprAfterEqual = false;

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

        /// <summary>現在の演算子種別を保持する変数</summary>
        private OperatorType mType = OperatorType.NON;

        /// <summary>
        /// フォームのコンストラクタ
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // ディスプレイサイズ固定化
            this.MaximumSize = this.Size;
            this.MinimumSize = this.Size;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // エラー時に無効化するボタン
            DisabledButtonsOnError = new Button[]
            {
                btnDot, btnTogglesign, btnPercent, btnPlus,
                btnMinus, btnMultiply, btnDivide, btnEnter
            };
        }

		/// <summary>
		/// フォームの初期化処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
        private void Form1_Load(object sender, EventArgs e)
        {
            textResult.Text = ZeroValue;
            TextOverwrite = true;

            textResult.Font = new Font(textResult.Font.FontFamily, WinResultBaseSize, textResult.Font.Style);
            textExpression.Font = new Font(textExpression.Font.FontFamily, WinExprBaseSize, textExpression.Font.Style);

            // 基準サイズの保持
            defaultFontSize = textResult.Font.Size;
            defaultExpressionFontSize = textExpression.Font.Size;

			// 計算結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
            textResult.ReadOnly = true;
            textResult.TextAlign = HorizontalAlignment.Right;
            textResult.BorderStyle = BorderStyle.None;

			// 途中結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
            textExpression.ReadOnly = true;
            textExpression.TextAlign = HorizontalAlignment.Right;
            textExpression.BorderStyle = BorderStyle.None;
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

        /// <summary>
        /// 
        /// </summary>
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
            Button btn = sender as Button;
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
            Button btn = (Button)sender;
            OperatorType op = OperatorType.NON;

            switch (btn.Text)
            {
                case AddSymbol:
                    {
                        op = OperatorType.ADD;
                        break;
                    }
                case SubtractSymbol:
                    {
                        op = OperatorType.SUBTRACT;
                        break;
                    }
                case MultiplySymbol:
                    {
                        op = OperatorType.MULTIPLY;
                        break;
                    }
                case DivideSymbol:
                    {
                        op = OperatorType.DIVIDE;
                        break;
                    }
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

        /// <summary>
        /// 数字キー入力のメイン処理
		/// エラー復帰、指数表示からの上書き開始、桁数制御、カンマ付与、
        /// 「±で末尾ゼロ保持」用の生文字列更新を行う。
        /// </summary>
        private void OnDigitButton(string digit)
        {
            HandleInitialState();
            SetButtonsEnabled(true);
            lastActionWasPercent = false;

            // 指数表示中は次入力で強制上書き
            if (IsExponentDisplay())
            {
                TextOverwrite = true;
                NumDot = false;
            }

            string current = textResult.Text.Replace(",", "");
            if (!IsInputValid(current, digit))
            {
                return;
            }

            if (TextOverwrite)
            {
                StartNewNumber(digit);
            }
            else
            {
                AppendDigit(digit);
            }

            UpdateTextResultWithCommas();
            IsClearEntry = false;

            preserveFormatOnToggle = true;
            lastUserTypedRaw = textResult.Text.Replace(",", "");
        }

        /// <summary>
        /// 小数点キー入力のメイン処理
		/// 0. からの開始、重複小数点抑止、
        /// 「±で末尾ゼロ保持」用生文字列の更新を行う。
        /// </summary>
        private void OnDotButton()
        {
            HandleInitialState();
            lastActionWasPercent = false;

            if (NumDot)
            {
                return;
            }

            if (TextOverwrite)
            {
                textResult.Text = "0.";
                TextOverwrite = false;
            }
            else
            {
                textResult.Text += ".";
            }
            NumDot = true;

            preserveFormatOnToggle = true;
            lastUserTypedRaw = textResult.Text.Replace(",", "");
        }

        /// <summary>
        /// 計算命令キーのメイン処理
        /// </summary>
        private void OnOperatorButton(OperatorType op)
        {
            if (IsError())
            {
                ResetCalculatorState();
                return;
            }

            try
            {
                if (lastActionWasPercent && mType != OperatorType.NON && !ExpressionEndsWithEqual())
                {
                    decimal cur = GetCurrentValue();
                    PerformPendingCalculation(cur);
                    if (IsError())
                    {
                        return;
                    }

                    DisplayNumber(FirstValue, true);
                    mType = op;
                    UpdateExpressionDisplay(FirstValue, mType);

                    lastActionWasPercent = false;
                    TextOverwrite = true;
                    NumDot = false;
                    return;
                }

                if (IsClearEntry)
                {
                    mType = op;
                    UpdateExpressionDisplay(FirstValue, mType);
                    DisplayNumber(FirstValue, true);
                    IsClearEntry = false;
                    return;
                }

                
                if (TextOverwrite && mType != OperatorType.NON && !ExpressionEndsWithEqual())
                {
                    mType = op;
                    UpdateExpressionDisplay(FirstValue, mType);
                }
                else
                {
                    decimal currentValue = GetCurrentValue();
                    PerformPendingCalculation(currentValue);
                    if (IsError())
                    {
                        return;
                    }

                    DisplayNumber(FirstValue, true);
                    mType = op;
                    UpdateExpressionDisplay(FirstValue, mType);
                }

                TextOverwrite = true;
                NumDot = false;
                lastActionWasPercent = false;
                preserveFormatOnToggle = false; 
            }
            catch (OverflowException)
            {
                SetErrorState(ErrMsgOverflow);
            }
        }

        /// <summary>
        /// イコールキーのメイン処理である。
        /// 0÷0と0除算 は規定メッセージでエラー化する。
        /// </summary>
        private void OnEqualsButton()
        {
            if (ShouldResetOnError())
            {
                return;
            }

            try
            {
                decimal result = ProcessEqualsLogic();
                if (IsError())
                {
                    return;
                }
                DisplayNumber(result, true);

              
                preserveFormatOnToggle = false;
                lastActionWasPercent = false;
            }
            catch (InvalidOperationException ex)
            {
                SetErrorState(ex.Message);
            }
            catch (OverflowException)
            {
                SetErrorState(ErrMsgOverflow);
            }
        }

        /// <summary>
        /// ％キーのメイン処理である。単項%（/100）と、演算子右辺%（加減は A*(B/100)、乗除は B/100）に対応する。
        /// 直後は演算子押下で確定するようフラグを設定する。
        /// </summary>
        private void OnPercentButton()
        {
            if (ShouldResetOnError())
            {
                return;
            }

            
            if (mType == OperatorType.NON)
            {
                decimal v = GetCurrentValue() * PercentMultiplier;
                DisplayNumber(v, false);

                lastActionWasPercent = true;
                preserveFormatOnToggle = false;
                return;
            }

            decimal currentValue = GetCurrentValue();
            decimal percentValue = CalculatePercent(currentValue);
            UpdatePercentDisplay(percentValue);

          
            lastActionWasPercent = true;
            preserveFormatOnToggle = false;
        }

        /// <summary>
        /// クリアエントリーキー入力の処理。式は維持し、表示を 0 に戻す。
        /// </summary>
        private void OnClearEntryButton()
        {
            if (ShouldResetOnError())
            {
                return;
            }

            IsClearEntry = true;
            textResult.Text = ZeroValue;
            TextOverwrite = true;
            NumDot = false;

            preserveFormatOnToggle = false;
            lastUserTypedRaw = ZeroValue;
            lastActionWasPercent = false;
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
                return;
            }

            if (ExpressionEndsWithEqual())
            {
                textExpression.Text = "";
                TextOverwrite = true;     
                NumDot = false;
                preserveFormatOnToggle = false;
                lastActionWasPercent = false;
                clearedExprAfterEqual = true;
                return;                       
            }

            if (clearedExprAfterEqual)
            {
                return;
            }

            // 指数表示中は新規入力開始扱い
            if (IsExponentDisplay())
            {
                TextOverwrite = true;
                NumDot = false;
                textResult.Text = ZeroValue;

                preserveFormatOnToggle = false;
                lastUserTypedRaw = ZeroValue;
                lastActionWasPercent = false;
                return;
            }

            if (TextOverwrite)
            {
                return;
            }

            HandleBackspace();
            UpdateTextResultWithCommas();

           
            preserveFormatOnToggle = true;
            lastUserTypedRaw = textResult.Text.Replace(",", "");
            lastActionWasPercent = false;
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

            if (string.IsNullOrEmpty(textResult.Text))
            {
                return;
            }

            if (preserveFormatOnToggle && !IsExponentDisplay())
            {
                string raw = textResult.Text.Replace(",", "");
                raw = ToggleSignRaw(raw);               
                SetTextFromRawPreservingCommas(raw);   

                
                TextOverwrite = false;
                NumDot = (raw.IndexOf('.') >= 0);

                
                UpdateExpressionForToggleSign();

               
                lastUserTypedRaw = raw;
                lastActionWasPercent = false;
                return;
            }

            // 計算表示のときは数値反転
            ToggleSign();                 
            UpdateExpressionForToggleSign();

            preserveFormatOnToggle = false;
            lastActionWasPercent = false;
        }


        /// <summary>
        /// 新しい数値入力を開始する。上書きモードを解除し、小数点フラグを更新。
        /// </summary>
        private void StartNewNumber(string digit)
        {
            textResult.Text = digit;
            TextOverwrite = false;
            NumDot = (digit == ".");
        }

        /// <summary>
        /// 既存の入力の末尾に数字を追加する。
        /// </summary>
        private void AppendDigit(string digit)
        {
            textResult.Text += digit;
        }

        /// <summary>
        /// 入力の妥当性を検証する。整数/小数の最大桁数、先頭0の扱いの抑止などを行う。
        /// </summary>
        private bool IsInputValid(string currentText, string digit)
        {
            bool startsWithZeroDot = currentText.StartsWith("0.") || currentText.StartsWith("-0.");
            int maxDigits = startsWithZeroDot ? DisplayMaxFractionDigitsLeadingZero : DisplayMaxIntegerDigits;

            string nextText = TextOverwrite ? digit : currentText + digit;
            int nextLength = nextText.Replace(".", "").Replace("-", "").Length;

            if (nextLength > maxDigits)
            {
                return false;
            }

            if (!TextOverwrite && currentText == ZeroValue && digit == ZeroValue && !NumDot)
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// 指定された演算子タイプに基づき、演算子の記号を返す。
        /// </summary>
        /// <param name="type">演算子のタイプである。</param>
        /// <returns>指定された演算子タイプに対応する記号である。</returns>
        private string GetOperatorSymbol(OperatorType type)
        {
            switch (type)
            {
                case OperatorType.ADD:
                    {
                        return AddSymbol;
                    }
                case OperatorType.SUBTRACT:
                    {
                        return SubtractSymbol;
                    }
                case OperatorType.MULTIPLY:
                    {
                        return MultiplySymbol;
                    }
                case OperatorType.DIVIDE:
                    {
                        return DivideSymbol;
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        /// <summary>
        /// 左右と演算子種別から結果を計算する（÷0 の例外処理は呼び出し側で行う）。
        /// </summary>
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
        /// 保留中の演算を解決する。未選択なら左辺を現在値に更新し、÷0/0÷0 はここでエラー化する。
        /// </summary>
        private void PerformPendingCalculation(decimal currentValue)
        {
            if (ExpressionEndsWithEqual() || mType == OperatorType.NON)
            {
                FirstValue = currentValue;
            }
            else
            {
                if (mType == OperatorType.DIVIDE && currentValue == InitialValue)
                {
                    if (FirstValue == InitialValue)
                    {
                        SetErrorState(ErrMsgUndefined);
                    }
                    else
                    {
                        SetErrorState(ErrMsgDivZero);
                    }
                    return;
                }

                decimal result = Calculate(FirstValue, currentValue, mType);
                FirstValue = result;
            }
        }

        /// <summary>
        /// 途中式表示を更新する
        /// </summary>
        private void UpdateExpressionDisplay(decimal value, OperatorType type)
        {
            string op = GetOperatorSymbol(type);
            textExpression.Text = string.Format("{0} {1}", FormatNumberForExpression(value), op);
        }

        /// <summary>
        ///イコールキーの処理
        /// </summary>
		private decimal ProcessEqualsLogic()
		{
			decimal currentValue = GetCurrentValue();
			bool isFirstEqual = !ExpressionEndsWithEqual();

			// 演算子未選択ならそのまま表示
			if (mType == OperatorType.NON)
			{
				SecondValue = currentValue;
				textExpression.Text = string.Format("{0} {1}", FormatNumberForExpression(currentValue), EqualSymbol);
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

			// ゼロ除算チェック
			if (mType == OperatorType.DIVIDE && right == InitialValue)
			{
				if (left == InitialValue)
				{
					throw new InvalidOperationException(ErrMsgUndefined);
				}
				else
				{
					throw new InvalidOperationException(ErrMsgDivZero);
				}
			}

			decimal result = Calculate(left, right, mType);


			FirstValue = result;

			textExpression.Text = string.Format("{0} {1} {2} {3}",
				FormatNumberForExpression(left),
				GetOperatorSymbol(mType),
				FormatNumberForExpression(right),
				EqualSymbol);

			return result;
		}

        /// <summary>
        /// 現在値を％（/100）へ変換する。
        /// </summary>
        private decimal CalculatePercent(decimal value)
        {
            return value * PercentMultiplier;
        }

        /// <summary>
        /// ％の表示と式を更新する。加減算は A*(B/100)、乗除算は B/100 として扱う。
        /// 表示は編集継続可能な状態にする。
        /// </summary>
        private void UpdatePercentDisplay(decimal percentValue)
        {
            decimal previousValue = FirstValue;
            decimal calculatedValue;

            if (mType == OperatorType.ADD || mType == OperatorType.SUBTRACT)
            {
                calculatedValue = previousValue * percentValue; 
                textExpression.Text = string.Format("{0} {1} {2}",
                    FormatNumberForExpression(previousValue),
                    GetOperatorSymbol(mType),
                    FormatNumberForExpression(calculatedValue));
            }
            else
            {
                calculatedValue = percentValue; // B% = B/100
                textExpression.Text = string.Format("{0} {1} {2}",
                    FormatNumberForExpression(previousValue),
                    GetOperatorSymbol(mType),
                    FormatNumberForExpression(calculatedValue));
            }


            DisplayNumber(calculatedValue, false);
        }

        /// <summary>
        /// 結果表示の末尾 1 文字を削除する。空や単独の「-」になった場合は 0 に戻す。
        /// </summary>
        private void HandleBackspace()
        {
            string currentText = textResult.Text.Replace(",", "");
            if (currentText.Length > 0)
            {
                string newText = currentText.Substring(0, currentText.Length - 1);

                if (string.IsNullOrEmpty(newText) || newText == "-")
                {
                    textResult.Text = ZeroValue;
                    TextOverwrite = true;
                    NumDot = false;
                }
                else
                {
                    textResult.Text = newText;
                    NumDot = textResult.Text.Contains(".");
                }
            }
            else
            {
                ResetCalculatorState();
            }
        }

        /// <summary>
        /// 数値として符号反転を行い表示を更新
        /// </summary>
        private void ToggleSign()
        {
            displayValue = GetCurrentValue();
            displayValue = -displayValue;
            DisplayNumber(displayValue, false);
			isNegated = true;
        }

        /// <summary>
        /// カンマ無しの文字列の符号のみを反転する
        /// </summary>
        private string ToggleSignRaw(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return raw;
            }
            if (raw[0] == '-')
            {
                return raw.Substring(1);
            }
            return "-" + raw;
        }

        /// <summary>
        /// 文字列をそのまま結果欄に反映し、カンマ付与を行う。
        /// </summary>
        private void SetTextFromRawPreservingCommas(string raw)
        {
            textResult.Text = raw;
            UpdateTextResultWithCommas();
        }

        /// <summary>
        /// イコールキー入力直後の サインチェンジキーを入力したとき negate(...) の入れ子表記で更新する。
        /// 例）"100 =" → "negate(100) =" → さらに ± → "negate(negate(100)) ="
        /// </summary>
        private void UpdateExpressionForToggleSign()
        {
            if (!ExpressionEndsWithEqual())
            {
                return;
            }

            string expr = textExpression.Text;
            int eq = expr.LastIndexOf(EqualSymbol);
            string body = (eq >= 0 ? expr.Substring(0, eq) : expr).Trim();

            if (string.IsNullOrEmpty(body))
            {
                body = FormatNumberForExpression(FirstValue);
            }

            string newBody = string.Format("{0}({1})", NegateFuncName, body);
            textExpression.Text = newBody + " " + EqualSymbol;
        }


        /// <summary>
        /// 表示の丸め、 カンマ付与 、 状態更新を行う。
        /// </summary>
        private void DisplayNumber(decimal value, bool overwrite)
        {
            decimal rounded = RoundResult(value);
            textResult.Text = FormatNumberForDisplay(rounded);
            UpdateTextResultWithCommas();
            TextOverwrite = overwrite;
            NumDot = false; // 上書きモードに入る時は必ず false

            // 計算由来の表示なので、±の見た目保持はオフ
            preserveFormatOnToggle = false;
            lastUserTypedRaw = textResult.Text.Replace(",", "");
        }

        /// <summary>
        /// 結果表示の文字列に 3 桁区切りカンマを付与する
        /// </summary>
        private void UpdateTextResultWithCommas()
        {
            if (IsError())
            {
                return;
            }
            if (IsExponentDisplay())
            {
                return;
            }

            string currentText = textResult.Text.Replace(",", "");
            if (string.IsNullOrEmpty(currentText) || currentText == "-" || (currentText == "0" && !NumDot))
            {
                return;
            }

            bool isNegative = currentText.StartsWith("-");
            if (isNegative)
            {
                currentText = currentText.Substring(1);
            }

            int dotIndex = currentText.IndexOf('.');
            string integerPart = currentText;
            string decimalPart = "";

            if (dotIndex != -1)
            {
                integerPart = currentText.Substring(0, dotIndex);
                decimalPart = currentText.Substring(dotIndex + 1);
            }

            try
            {
                decimal integerValue;
                if (decimal.TryParse(integerPart, NumberStyles.Number, CultureInfo.InvariantCulture, out integerValue))
                {
                    string formattedInteger = integerValue.ToString("#,##0", CultureInfo.InvariantCulture);
                    string newText = formattedInteger;

                    if (dotIndex != -1)
                    {
                        newText += "." + decimalPart;
                    }
                    if (isNegative)
                    {
                        newText = "-" + newText;
                    }

                    textResult.Text = newText;
                }
            }
            catch (FormatException)
            {
                // 何もしない
            }
        }

        /// <summary>
        /// 途中式用の数値整形を行う。
        /// </summary>
        private string FormatNumberForExpression(decimal value)
        {
            return FormatNumberForDisplay(value);
        }

        /// <summary>
        /// 指数表記の整形を行う。指数は小文字 e、不要な 0/小数点を整理する。
        /// </summary>
        private string FormatExponential(decimal value)
        {
            string gFormat = value.ToString("G15", CultureInfo.InvariantCulture);
            string expString = gFormat.Contains("E")
                ? gFormat
                : decimal.Parse(gFormat, CultureInfo.InvariantCulture).ToString("E", CultureInfo.InvariantCulture);

            expString = expString.Replace("E+", "e+").Replace("E-", "e-");
            string[] parts = expString.Split(new char[] { 'e' });

            string mantissa = parts[0].TrimEnd('0');
            if (mantissa.EndsWith("."))
            {
                mantissa = mantissa.TrimEnd('.');
            }
            if (!mantissa.Contains("."))
            {
                mantissa += ".";
            }

            string exponent = Regex.Replace(parts[1], @"^(\+|-)(0)(\d+)", "$1$3");
            return mantissa + "e" + exponent;
        }

        /// <summary>
        /// 数値の大きさが表示可能な桁数を超える場合、指数表記に変換する
        /// </summary>
        private string FormatNumberForDisplay(decimal value)
        {
            decimal abs = Math.Abs(value);
            if (abs == 0m)
            {
                return ZeroValue;
            }

           
            string fixedStr = value.ToString("0.#############################", CultureInfo.InvariantCulture);

            if (abs >= 1m)
            {
               
                int dot = fixedStr.IndexOf('.');
                bool neg = (fixedStr[0] == '-');
                int intLen = (dot >= 0 ? dot : fixedStr.Length) - (neg ? 1 : 0);

                if (intLen > DisplayMaxIntegerDigits)
                {
                    return FormatExponential(value);
                }
                return fixedStr;
            }
            else
            {
                
                int dot = fixedStr.IndexOf('.');
                int fracLen = (dot >= 0) ? (fixedStr.Length - dot - 1) : 0;

                if (fracLen > DisplayMaxFractionDigitsLeadingZero)
                {
                    return FormatExponential(value);
                }
                return fixedStr;
            }
        }

        /// <summary>
        /// 現在の表示文字列を decimal に変換する
        /// </summary>
        private decimal GetCurrentValue()
        {
            return ParseDisplayToDecimal(textResult.Text);
        }

        /// <summary>
        /// 表示文字列を decimal に変換する
        /// </summary>
        private decimal ParseDisplayToDecimal(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return InitialValue;
            }

            string s = text.Replace(",", "");
            decimal dv;

            if (s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0)
            {
                double dd;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out dd))
                {
                    try
                    {
                        return (decimal)dd;
                    }
                    catch
                    {
                        return InitialValue;
                    }
                }
                return InitialValue;
            }

            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
            {
                return dv;
            }
            return InitialValue;
        }

        /// <summary>
		/// 数値の絶対値に応じて計算結果を丸める
        /// </summary>
        private decimal RoundResult(decimal value)
        {
            decimal abs = Math.Abs(value);

         
            if (abs > 0m && abs < 1m)
            {
                return Math.Round(value, 17, MidpointRounding.AwayFromZero);
            }

            if (abs >= 1m)
            {
                string integerPartStr = Math.Floor(abs).ToString(CultureInfo.InvariantCulture);
                int integerLength = integerPartStr.Length;

                int decimalPlacesToRound = 16 - integerLength;
                if (decimalPlacesToRound >= 0)
                {
                    return Math.Round(value, decimalPlacesToRound, MidpointRounding.AwayFromZero);
                }
            }

            return value;
        }


        /// <summary>
        /// エラー状態かどうかを返す。
        /// </summary>
        private bool IsError()
        {
            return IsErrorState;
        }

        /// <summary>
        /// 式表示が ＝ で終わっているか判定する
        /// </summary>
        private bool ExpressionEndsWithEqual()
        {
            return textExpression.Text.Length > 0 && textExpression.Text.EndsWith(EqualSymbol);
        }

        /// <summary>
        /// 現在の結果表示が指数表記か判定する。
        /// </summary>
        private bool IsExponentDisplay()
        {
            string t = textResult.Text;
            return (t.IndexOf('e') >= 0 || t.IndexOf('E') >= 0);
        }

        /// <summary>
        /// 
        /// </summary>
        private bool ShouldResetOnError()
        {
            if (IsErrorState)
            {
                ResetCalculatorState();
                return true;
            }
            return false;
        }

        /// <summary>
		/// 結果表示欄のフォントサイズを初期化
        /// </summary>
        private void AutoFitResultFont()
        {
            float size = defaultFontSize; // 基準サイズ

            FontFamily family = textResult.Font.FontFamily;
            FontStyle style = textResult.Font.Style;

            while (size > WinMinFontSize)
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
                size -= WinFontStep;
            }

            if (Math.Abs(textResult.Font.Size - WinMinFontSize) > 0.1f)
            {
                Font oldFinal = textResult.Font;
                textResult.Font = new Font(family, WinMinFontSize, style);
                oldFinal.Dispose();
            }
        }

        /// <summary>
        /// エラー時に一部ボタンの活性/非活性をまとめて切り替える。
        /// </summary>
        private void SetButtonsEnabled(bool enabled)
        {
            foreach (Button btn in DisabledButtonsOnError)
            {
                btn.Enabled = enabled;
            }
        }

        /// <summary>
        /// エラー状態を設定し、メッセージ表示・フォントサイズ調整・ボタン無効化を行う。
        /// </summary>
        private void SetErrorState(string message)
        {
            textResult.Text = message;

            float sz = ErrorFontSize;
            if (sz < WinMinFontSize)
            {
                sz = WinMinFontSize;
            }
            textResult.Font = new Font(textResult.Font.FontFamily, sz, textResult.Font.Style);

            IsErrorState = true;
            SetButtonsEnabled(false);
        }

        /// <summary>
        /// 入力開始前の状態を整える。エラー中や ＝直後の場合は状態を初期化する。
        /// </summary>
        private void HandleInitialState()
        {
            if (IsErrorState || ExpressionEndsWithEqual())
            {
                ResetCalculatorState();
                clearedExprAfterEqual = false;
            }
        }

        /// <summary>
        /// 全体のリセット処理である。計算状態と UI 状態を初期化し、ボタンを活性化する。
        /// </summary>
        private void ResetAllState()
        {
            ResetCalculatorState();
            SetButtonsEnabled(true);
        }

        /// <summary>
        /// 計算機内部状態と表示、フォントの初期化を行う
        /// </summary>
        private void ResetCalculatorState()
        {
            FirstValue = InitialValue;
            SecondValue = InitialValue;
            mType = OperatorType.NON;
            textExpression.Text = "";
            textResult.Text = ZeroValue;
            TextOverwrite = true;
            NumDot = false;
            IsErrorState = false;
            IsClearEntry = false;
            displayValue = InitialValue;

            preserveFormatOnToggle = false;
            lastUserTypedRaw = ZeroValue;
            lastActionWasPercent = false;
            clearedExprAfterEqual = false;

            // フォントを基準サイズに戻す
           
            textResult.Font = new Font(textResult.Font.FontFamily, defaultFontSize, textResult.Font.Style);

           
            textExpression.Font = new Font(textExpression.Font.FontFamily, defaultExpressionFontSize, textExpression.Font.Style);

			//結果表示欄のフォントサイズを初期化
            AutoFitResultFont();
        }
    }
}
